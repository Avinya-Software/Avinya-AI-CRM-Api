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

        public async Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isSuperAdmin, List<string> allowedModules)
        {
            var apiKey = _config["Gemini:ApiKey"];

            // 1. Local Keyword Picker + Permission Filtering (to build targeted schema)
            var lowerMessage = userMessage.ToLower();
            var finalTables = new HashSet<string>();

            var mapping = new Dictionary<string, string[]>
            {
                { "lead", new[] { "Leads", "LeadFollowups", "LeadSourceMaster", "LeadStatusMaster", "Clients" } },
                { "leads", new[] { "Leads", "LeadFollowups", "LeadSourceMaster", "LeadStatusMaster", "Clients" } },
                { "enquiry", new[] { "Leads", "LeadFollowups" } },
                { "enquiries", new[] { "Leads", "LeadFollowups" } },
                { "inquiry", new[] { "Leads", "LeadFollowups" } },
                { "inquiries", new[] { "Leads", "LeadFollowups" } },
                { "followup", new[] { "Leads", "LeadFollowups" } },
                { "follow up", new[] { "Leads", "LeadFollowups" } },
                { "follow-up", new[] { "Leads", "LeadFollowups" } },
                { "client", new[] { "Clients", "States", "Cities" } },
                { "clients", new[] { "Clients", "States", "Cities" } },
                { "customer", new[] { "Clients" } },
                { "customers", new[] { "Clients" } },
                { "order", new[] { "Orders", "OrderItems", "OrderStatusMaster", "Products", "Clients" } },
                { "orders", new[] { "Orders", "OrderItems", "OrderStatusMaster", "Products", "Clients" } },
                { "booking", new[] { "Orders", "OrderItems" } },
                { "bookings", new[] { "Orders", "OrderItems" } },
                { "quotation", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "quotations", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "quote", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "quotes", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "proposal", new[] { "Quotations", "QuotationItems" } },
                { "proposals", new[] { "Quotations", "QuotationItems" } },
                { "product", new[] { "Products", "TaxCategoryMaster", "UnitTypeMaster" } },
                { "products", new[] { "Products", "TaxCategoryMaster", "UnitTypeMaster" } },
                { "item", new[] { "Products" } },
                { "items", new[] { "Products" } },
                { "expense", new[] { "Expenses", "ExpenseCategories" } },
                { "expenses", new[] { "Expenses", "ExpenseCategories" } },
                { "spend", new[] { "Expenses", "ExpenseCategories" } },
                { "revenue", new[] { "Orders", "Quotations" } },
                { "sales", new[] { "Orders", "Quotations" } },
                { "project", new[] { "Projects", "ProjectStatusMaster", "ProjectPriorityMaster", "Clients" } },
                { "projects", new[] { "Projects", "ProjectStatusMaster", "ProjectPriorityMaster", "Clients" } },
                { "team", new[] { "Teams", "AspNetUsers" } },
                { "teams", new[] { "Teams", "AspNetUsers" } },
                { "user", new[] { "AspNetUsers" } },
                { "users", new[] { "AspNetUsers" } },
                { "staff", new[] { "AspNetUsers", "Teams" } },
                { "employee", new[] { "AspNetUsers" } },
                { "employees", new[] { "AspNetUsers" } },
                { "task", new[] { "TaskSeries", "TaskOccurrences", "TaskLists" } },
                { "tasks", new[] { "TaskSeries", "TaskOccurrences", "TaskLists" } },
                { "todo", new[] { "TaskSeries", "TaskOccurrences" } },
                { "tenant", new[] { "Tenants" } },
                { "tenants", new[] { "Tenants" } },
                { "company", new[] { "Tenants", "Clients" } },
                { "companies", new[] { "Tenants", "Clients" } },
                { "location", new[] { "Cities", "States", "Clients" } }
            };

            var baseTables = new HashSet<string> { 
                "LeadSourceMaster", "LeadStatusMaster", "LeadFollowupStatus", 
                "OrderStatusMaster", "DesignStatusMaster", "QuotationStatusMaster", 
                "ProjectStatusMaster", "ProjectPriorityMaster", 
                "TaxCategoryMaster", "States", "Cities", "AspNetUsers" 
            };

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

            var deniedModules = new HashSet<string>();
            foreach (var entry in mapping)
            {
                if (lowerMessage.Contains(entry.Key))
                {
                    bool hasAtLeastOneFunctionalTable = false;
                    foreach (var table in entry.Value)
                    {
                        if (baseTables.Contains(table)) { finalTables.Add(table); continue; }
                        if (isSuperAdmin) { finalTables.Add(table); hasAtLeastOneFunctionalTable = true; continue; }

                        var module = moduleTableMap.FirstOrDefault(x => x.Value.Contains(table)).Key;
                        if (module != null && allowedModules.Contains(module))
                        {
                            finalTables.Add(table);
                            hasAtLeastOneFunctionalTable = true;
                        }
                    }
                    if (!isSuperAdmin && !hasAtLeastOneFunctionalTable && moduleTableMap.ContainsKey(entry.Key) && !allowedModules.Contains(entry.Key))
                    {
                        deniedModules.Add(entry.Key);
                    }
                }
            }

            if (!isSuperAdmin && deniedModules.Any())
            {
                return new AIResponse { Action = "message", ErrorMessage = $"Access Denied: You do not have permission to view {string.Join(", ", deniedModules)}." };
            }

            var targetedSchema = finalTables.Any() ? AISchema.GetTables(finalTables) : (isSuperAdmin ? AISchema.CRM : AISchema.GetTables(baseTables));

            var securityRule = isSuperAdmin 
                ? "1. You are a SUPER ADMIN. You have global access. Do NOT add TenantId filters unless the user asks for a specific tenant."
                : "1. You are a per-tenant analyst.\n2. ONLY use 'WHERE TenantId = @TenantId' for tables that explicitly include 'TenantId'.\n3. Join with parent tables if needed to filter by TenantId.";

            var prompt = $@"
                You are a CRM assistant. Analyze input and return ONLY valid JSON.
                
                ACTIONS:
                - ""create_lead"": User wants to create a lead. Extract 'CompanyName', 'Mobile', 'Email', 'Notes'.
                - ""get_summary"": User wants a report or info. Generate a T-SQL SELECT query.
                - ""message"": General conversation or fallback.

                SQL RULES (MANDATORY for ""get_summary""):
                1. {securityRule}
                2. SECURITY (CRITICAL): If you are NOT a super admin, you MUST include 'WHERE TenantId = @TenantId' in your query. If you join multiple tables, ensure at least one table is filtered by TenantId.
                3. JOIN LOGIC (CRITICAL): Always join on ID/GUID columns (e.g., ClientID, LeadStatusID, LeadID). NEVER join on name columns.
                4. READABLE DATA: In the SELECT clause, prefer human-readable columns (e.g., CompanyName, StatusName, FullName) over IDs.
                5. USER LOOKUP: Join with 'dbo.AspNetUsers' on 'Id' to show 'FullName' for columns like 'CreatedBy' or 'AssignedTo'.
                6. SCHEMAS: Always use the 'dbo.' prefix (e.g., 'dbo.Leads').
                7. Schema Context:
                {targetedSchema}

                JSON FORMAT:
                {{
                  ""action"": ""create_lead"" | ""get_summary"" | ""message"",
                  ""parameters"": {{ ""CompanyName"": ""..."", ""Mobile"": ""..."", ... }},
                  ""sql"": ""SELECT ..."",
                  ""successMessage"": ""Found {{count}} records."",
                  ""errorMessage"": ""No records found.""
                }}

                User Input: {userMessage}";

            var result = await CallGeminiAsync(prompt, apiKey);
            if (string.IsNullOrEmpty(result)) return new AIResponse { Action = "message", ErrorMessage = "AI service error." };

            try 
            {
                var clean = CleanJsonResponse(result);
                return JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();
            }
            catch 
            {
                return new AIResponse { Action = "message", ErrorMessage = "Error parsing AI response." };
            }
        }

        private async Task<string> CallGeminiAsync(string prompt, string apiKey)
        {
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );
            var result = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(result);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return "";
            return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }

        private string CleanJsonResponse(string text)
        {
            text = text.Replace("```json", "").Replace("```", "").Trim();
            var start = text.IndexOf("{");
            var end = text.LastIndexOf("}") + 1;
            if (start == -1 || end == -1) return "{}";
            return text.Substring(start, end - start);
        }
    }
}
