using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Domain.Constant;
using AvinyaAICRM.Shared.AI;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AvinyaAICRM.Infrastructure.Repositories
{
    public class GeminiService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GeminiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<AIResponse> GetIntentAsync(string userMessage)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var prompt = $@"
                        You are a CRM assistant.
                            Use this database schema:

                            {AISchema.CRM}

                        Return ONLY valid JSON.
                        Do NOT use markdown.
                        Do NOT add ```json or ```.
                        If user gives specific date → use FromDate and ToDate
                        If user gives range → use both FromDate and ToDate
                        If user says today/yesterday → use dateRange

                        Format:
                        {{ ""action"": ""get_summary"", ""dateRange"": ""last_7_days"" }}

                        User Input: {userMessage}
                        ";

            var result = await CallGeminiAsync(prompt, apiKey);
            var cleanJson = CleanJsonResponse(result);

            return JsonSerializer.Deserialize<AIResponse>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<SQLAIResponse> GenerateSqlAsync(string userMessage, Guid tenantId, bool isSuperAdmin, List<string> allowedModules)
        {
            var apiKey = _config["Gemini:ApiKey"];

            // 1. Local Keyword Picker + Permission Filtering
            var lowerMessage = userMessage.ToLower();
            var selectedTables = new HashSet<string>();

            var mapping = new Dictionary<string, string[]>
            {
                { "lead", new[] { "Leads", "LeadFollowups", "LeadSourceMaster", "LeadStatusMaster", "Clients" } },
                { "followup", new[] { "Leads", "LeadFollowups" } },
                { "follow up", new[] { "Leads", "LeadFollowups" } },
                { "client", new[] { "Clients", "States", "Cities" } },
                { "customer", new[] { "Clients" } },
                { "order", new[] { "Orders", "OrderItems", "OrderStatusMaster", "Products", "Clients" } },
                { "quote", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "product", new[] { "Products", "TaxCategoryMaster" } },
                { "expense", new[] { "Expenses", "ExpenseCategories" } },
                { "spend", new[] { "Expenses", "ExpenseCategories" } },
                { "revenue", new[] { "Orders", "Quotations" } },
                { "sales", new[] { "Orders", "Quotations" } },
                { "project", new[] { "Projects", "ProjectStatusMaster", "ProjectPriorityMaster", "Clients" } },
                { "team", new[] { "Teams", "AspNetUsers" } },
                { "user", new[] { "AspNetUsers" } },
                { "staff", new[] { "AspNetUsers", "Teams" } },
                { "location", new[] { "Cities", "States", "Clients" } }
            };
            // 1. Define Base/Master Tables (Always allowed for context)
            var baseTables = new HashSet<string> { 
                "LeadSourceMaster", "LeadStatusMaster", "LeadFollowupStatus", 
                "OrderStatusMaster", "DesignStatusMaster", "QuotationStatusMaster", 
                "ProjectStatusMaster", "ProjectPriorityMaster", 
                "TaxCategoryMaster", "States", "Cities", "AspNetUsers" 
            };
            var finalTables = new HashSet<string>();

            // Module-to-Table mapping for permission boundary (match user's database ModuleKey)
            var moduleTableMap = new Dictionary<string, string[]>
            {
                { "lead", new[] { "Leads", "LeadFollowups" } },
                { "followup", new[] { "LeadFollowups" } },
                { "task", new[] { "TaskSeries", "TaskOccurrences", "TaskLists" } },
                { "quotation", new[] { "Quotations", "QuotationItems" } },
                { "order", new[] { "Orders", "OrderItems" } },
                { "invoice", new[] { "Orders" } },
                { "client", new[] { "Clients" } },
                { "product", new[] { "Products" } },
                { "project", new[] { "Projects" } },
                { "expense", new[] { "Expenses", "ExpenseCategories" } },
                { "team", new[] { "Teams" } },
                { "user", new[] { "AspNetUsers" } },
                { "settings", new[] { "Settings" } }
            };

            // 2. keyword-based selection with STRICT PERMISSION CHECK
            var deniedModules = new HashSet<string>();
            foreach (var entry in mapping)
            {
                if (lowerMessage.Contains(entry.Key))
                {
                    bool hasAtLeastOneFunctionalTable = false;
                    foreach (var table in entry.Value)
                    {
                        // 1. Always allow base tables
                        if (baseTables.Contains(table))
                        {
                            finalTables.Add(table);
                            continue;
                        }

                        // 2. Check permissions for functional tables
                        if (isSuperAdmin)
                        {
                            finalTables.Add(table);
                            hasAtLeastOneFunctionalTable = true;
                            continue;
                        }

                        var module = moduleTableMap.FirstOrDefault(x => x.Value.Contains(table)).Key;
                        if (module != null)
                        {
                            if (allowedModules.Contains(module))
                            {
                                finalTables.Add(table);
                                hasAtLeastOneFunctionalTable = true;
                            }
                        }
                    }

                    // 3. Security Check: Only trigger denial for non-admins
                    if (!isSuperAdmin && !hasAtLeastOneFunctionalTable && moduleTableMap.ContainsKey(entry.Key))
                    {
                        if (!allowedModules.Contains(entry.Key))
                        {
                            deniedModules.Add(entry.Key);
                        }
                    }
                }
            }

            // 3. Security Boundary: If no tables matched or common user, never provide the FULL schema.
            // SuperAdmins get the full schema if they didn't specify keywords. Regular users get the BASE schema.
            var targetedSchema = "";
            if (finalTables.Any())
            {
                targetedSchema = AISchema.GetTables(finalTables);
            }
            else
            {
                targetedSchema = isSuperAdmin ? AISchema.CRM : AISchema.GetTables(baseTables);
            }

            // 4. Permission Feedback for AI (Direct denial if user restricted)
            if (!isSuperAdmin && deniedModules.Any())
            {
                return new SQLAIResponse 
                { 
                    Sql = "", 
                    SuccessMessage = "", 
                    ErrorMessage = $"Access Denied: You do not have permission to view {string.Join(", ", deniedModules)}." 
                };
            }

            // 2. Generate SQL + Templates (Refined Prompt for SuperAdmin/Permissions)
            var securityRule = isSuperAdmin 
                ? "1. You are a SUPER ADMIN. You have global access. Do NOT add TenantId filters unless the user asks for a specific tenant."
                : $@"1. You are a per-tenant analyst.
                        2. ONLY use 'WHERE TenantId = @TenantId' for tables that explicitly include 'TenantId' in the schema below.
                        3. If a table doesn't have 'TenantId', ensure it is filtered via a JOIN to its parent table that DOES have 'TenantId'.";

            var sqlPrompt = $@"
                        You are a SQL expert for a CRM system. 
                        Task: Generate a T-SQL SELECT query AND natural language templates in JSON format.
                        
                        CRITICAL SECURITY RULES:
                        {securityRule}
                        
                        GENERAL RULES:
                        1. SELECT only. Never UPDATE, DELETE, DROP, INSERT, or ALTER.
                        2. PREFER READABLE DATA: Always JOIN with master tables (Clients, LeadStatusMaster, etc.) to return names instead of raw GUIDs. Join with 'AspNetUsers' on 'Id' to show 'FullName' for ALL user-related columns like 'AssignedTo', 'CreatedBy', 'FollowUpBy', 'ManagerId', 'AssignedDesignTo', 'ProjectManagerId', and 'AssignedToUserId'. All of these columns store the user's Id.
                        3. DATE FILTERING (CRITICAL): To filter by today/yesterday/recent, use the 'CreatedDate' or 'CreatedAt' column. These columns ARE available in the schema below for most tables.
                        4. SCHEMAS: Use the table names exactly as provided in the schema below.
                        5. FILTERING (CRITICAL): When filtering by a specific status, source, or category (e.g., 'today's leads', 'New leads', 'Client ABC'), NEVER compare string names directly against ID/GUID columns (like 'Status', 'LeadSource', 'ClientID'). You MUST JOIN with the correct master table and filter by the name column (e.g., StatusName = 'New' or CompanyName = 'ABC').
                        6. Schema: {targetedSchema}
                        7. TYPE CASTING (CRITICAL): In the 'Leads' table, 'Status' and 'LeadSource' are [nvarchar] strings. To join them with master tables (like LeadStatusMaster), you MUST use TRY_CAST. Example: 'ON TRY_CAST(L.Status AS uniqueidentifier) = LSMT.LeadStatusID'.
                        
                        RESPONSE FORMAT (JSON ONLY):
                        {{
                          ""sql"": ""The T-SQL query string"",
                          ""successMessage"": ""A friendly message summary. Use {{count}} as a placeholder for the number of records found."",
                          ""errorMessage"": ""A friendly message for when NO data is found.""
                        }}
                        
                        User Request: {userMessage}
                        ";

            var rawResult = await CallGeminiAsync(sqlPrompt, apiKey);
            if (string.IsNullOrEmpty(rawResult)) return new SQLAIResponse { ErrorMessage = "Communication error with AI service." };
            
            // Clean AI JSON (remove ```json wrappers if present)
            var cleanedJson = rawResult.Replace("```json", "").Replace("```", "").Trim();
            
            try 
            {
                return JsonSerializer.Deserialize<SQLAIResponse>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch 
            {
                // Fallback for malformed JSON
                return new SQLAIResponse { 
                    Sql = cleanedJson.Contains("SELECT") ? cleanedJson : "", 
                    SuccessMessage = "Here is what I found:", 
                    ErrorMessage = "I couldn't find any records." 
                };
            }
        }

        private async Task<string> CallGeminiAsync(string prompt, string apiKey)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            
            // Safety check for candidates (using TryGetProperty for dictionary safety)
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) 
            {
                return "";
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content) || 
                !content.TryGetProperty("parts", out var parts) || 
                parts.GetArrayLength() == 0) 
            {
                return "";
            }

            if (!parts[0].TryGetProperty("text", out var text))
            {
                return "";
            }

            return text.GetString() ?? "";
        }

        private string CleanJsonResponse(string text)
        {
            text = text.Replace("```json", "")
                       .Replace("```", "")
                       .Trim();

            var start = text.IndexOf("{");
            var end = text.LastIndexOf("}") + 1;

            if (start == -1 || end == -1)
            {
                throw new Exception("Invalid AI response format: " + text);
            }

            return text.Substring(start, end - start);
        }
    }
}
