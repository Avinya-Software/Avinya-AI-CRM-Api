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

            // 1. Keyword Identification
            var lowerMessage = userMessage.ToLower();
            var finalTables = new HashSet<string>();

            var mapping = new Dictionary<string, string[]>
            {
                { "lead", new[] { "Leads", "LeadFollowups", "LeadSourceMaster", "LeadStatusMaster", "Clients" } },
                { "leads", new[] { "Leads", "LeadFollowups", "LeadSourceMaster", "LeadStatusMaster", "Clients" } },
                { "followup", new[] { "Leads", "LeadFollowups" } },
                { "follow up", new[] { "Leads", "LeadFollowups" } },
                { "follow-up", new[] { "Leads", "LeadFollowups" } },
                { "follow", new[] { "Leads", "LeadFollowups" } },
                { "client", new[] { "Clients", "States", "Cities" } },
                { "clients", new[] { "Clients", "States", "Cities" } },
                { "customer", new[] { "Clients" } },
                { "customers", new[] { "Clients" } },
                { "order", new[] { "Orders", "OrderItems", "OrderStatusMaster", "Products", "Clients" } },
                { "orders", new[] { "Orders", "OrderItems", "OrderStatusMaster", "Products", "Clients" } },
                { "booking", new[] { "Orders", "OrderItems" } },
                { "quotation", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "quotations", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "quote", new[] { "Quotations", "QuotationItems", "QuotationStatusMaster", "Leads", "Clients" } },
                { "product", new[] { "Products", "TaxCategoryMaster", "UnitTypeMaster" } },
                { "expense", new[] { "Expenses", "ExpenseCategories" } },
                { "expense category", new[] { "ExpenseCategories" } },
                { "spend", new[] { "Expenses", "ExpenseCategories" } },
                { "revenue", new[] { "Orders", "Quotations" } },
                { "sales", new[] { "Orders", "Quotations" } },
                { "project", new[] { "Projects", "ProjectStatusMaster", "ProjectPriorityMaster", "Clients" } },
                { "team", new[] { "Teams", "AspNetUsers" } },
                { "user", new[] { "AspNetUsers" } },
                { "task", new[] { "TaskSeries", "TaskOccurrences", "TaskLists" } },
                { "todo", new[] { "TaskSeries", "TaskOccurrences" } },
                { "overall", new[] { "Leads", "Quotations", "Orders", "Expenses", "Projects", "TaskSeries", "TaskOccurrences", "LeadFollowups" } },
                { "report", new[] { "Leads", "Quotations", "Orders", "Expenses", "Projects", "TaskSeries", "TaskOccurrences", "LeadFollowups" } },
                { "summary", new[] { "Leads", "Quotations", "Orders", "Expenses", "Projects", "TaskSeries", "TaskOccurrences", "LeadFollowups" } },
                { "activity", new[] { "Leads", "LeadFollowups", "TaskOccurrences", "Orders" } }
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
                { "task", new[] { "TaskSeries", "TaskOccurrences", "TaskLists" } },
                { "quotation", new[] { "Quotations", "QuotationItems" } },
                { "order", new[] { "Orders", "OrderItems" } },
                { "client", new[] { "Clients" } },
                { "product", new[] { "Products" } },
                { "project", new[] { "Projects" } },
                { "expense", new[] { "Expenses", "ExpenseCategories" } },
                { "team", new[] { "Teams" } },
                { "user", new[] { "AspNetUsers" } }
            };

            foreach (var entry in mapping)
            {
                if (lowerMessage.Contains(entry.Key))
                {
                    foreach (var table in entry.Value)
                    {
                        if (baseTables.Contains(table)) { finalTables.Add(table); continue; }
                        if (isSuperAdmin) { finalTables.Add(table); continue; }

                        var module = moduleTableMap.FirstOrDefault(x => x.Value.Contains(table)).Key;
                        if (module != null && allowedModules.Contains(module)) finalTables.Add(table);
                    }
                }
            }

            var targetedSchema = finalTables.Any() ? AISchema.GetTables(finalTables) : (isSuperAdmin ? AISchema.CRM : AISchema.GetTables(baseTables));
            bool isReportMode = lowerMessage.Contains("report") || lowerMessage.Contains("summary") || lowerMessage.Contains("overall");
            if (isReportMode) targetedSchema = AISchema.CRM;

            var securityRule = isSuperAdmin 
                ? "1. SUPER ADMIN. Global access. Do NOT add TenantId filters unless specific." 
                : "1. Per-tenant analyst.\n2. Use 'WHERE TenantId = @TenantId'.";

            var prompt = $@"
                You are a Business Analyst assistant for a CRM system. 
                Your task is to analyze data and provide friendly, human-interactive insights.

                SQL RULES:
                1. {securityRule}
                2. SECURITY: Include '@TenantId'. Ignore records with 'IsDeleted = 1'.
                3. JOIN LOGIC: Join on ID/GUID columns, SELECT readable Names.
                4. COLUMN NAMES: Clients table uses 'CompanyName'.

                MODE: BUSINESS ANALYST MODE (for reports/summaries):
                - Create a structured, conversational report in Markdown.
                - Use placeholders like {{FieldName}} for database values. 
                - Be encouraging! If conversion is up, say ""Great work!"". If revenue is low, say ""Let's focus on converting some quotes today."".
                - Explain what the numbers mean for the business.

                MODE: DATA LIST MODE (for specific record lists):
                - Keep the message simple: ""Found {userMessage} records.""
                
                JSON FORMAT:
                {{
                  ""action"": ""create_lead"" | ""get_summary"" | ""message"",
                  ""parameters"": {{ ... }},
                  ""sql"": ""SELECT ... (if summary, return one row with all calculated fields)"",
                  ""successMessage"": ""Conversational, interactive message with {{Field}} placeholders."",
                  ""errorMessage"": ""No records found.""
                }}

                Schema Context:
                {targetedSchema}

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
