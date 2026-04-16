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

        public async Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isAdmin, List<string> allowedModules)
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
                { "activity", new[] { "Leads", "LeadFollowups", "TaskOccurrences", "Orders" } },
                { "invoice", new[] { "Invoices", "InvoiceStatuses", "Clients", "Orders" } },
                { "invoices", new[] { "Invoices", "InvoiceStatuses", "Clients", "Orders" } },
                { "billing", new[] { "Invoices", "Payments" } },
                { "receive payment", new[] { "Payments", "Invoices", "BankDetails" } },
                { "payment", new[] { "Payments", "Invoices", "BankDetails" } },
                { "payments", new[] { "Payments", "Invoices", "BankDetails" } }
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
                { "user", new[] { "AspNetUsers" } },
                { "invoice", new[] { "Invoices" } },
                { "payment", new[] { "Payments" } }
            };

            foreach (var entry in mapping)
            {
                if (lowerMessage.Contains(entry.Key))
                {
                    foreach (var table in entry.Value)
                    {
                        if (baseTables.Contains(table)) { finalTables.Add(table); continue; }
                        if (isAdmin) { finalTables.Add(table); continue; }

                        var module = moduleTableMap.FirstOrDefault(x => x.Value.Contains(table)).Key;
                        if (module != null && allowedModules.Contains(module)) finalTables.Add(table);
                    }
                }
            }

            var targetedSchema = finalTables.Any() ? AISchema.GetTables(finalTables) : (isAdmin ? AISchema.CRM : AISchema.GetTables(baseTables));
            bool isReportMode = lowerMessage.Contains("report") || lowerMessage.Contains("summary") || lowerMessage.Contains("overall");
            if (isReportMode) targetedSchema = AISchema.CRM;

             var currentTimeContext = $"Current Date/Time: {DateTime.Now:f}. Use this for all relative time queries like 'today', 'last week', etc.";

             var prompt = $@"
                You are a CRM Intent Parser. Analyze the user's input and return ONLY valid JSON.
                {currentTimeContext}

                STRICT RULES:
                1. DO NOT GENERATE SQL. The backend handles all database logic.
                2. NEVER name database tables or columns directly.
                3. Return ONLY the intent, type, entities, and filters in the JSON.
                4. Use the provided Current Date/Time context for ALL relative time calculations.

                ACTIONS:
                1. ""create_lead"": Extract 'CompanyName', 'Mobile', 'Email', 'Notes', 'ClientType' (Company/Individual, default is Company).
                2. ""create_task"": User wants to create a task. Extract: 'Title', 'Description', 'Notes', 'TaskScope' (Personal/Team), 'DueDateTime'.
                3. ""get_data"": Map user request to one or more conceptual entities.
                   - ENTITIES: ""Leads"", ""Clients"", ""Quotations"", ""Orders"", ""Expenses"", ""Invoices"", ""Projects"", ""LeadFollowups"".
                   - TYPES: 
                     - ""LIST"": Specific records (e.g., ""show"", ""list"", ""get"", ""find"").
                     - ""SUMMARY"": Aggregates or dashboard reports (e.g., ""summary"", ""report"", ""overall"", ""dashboard"", ""total"", ""count"").
                       IF THE USER ASKS FOR AN ""OVERALL"" OR ""DASHBOARD"" SUMMARY WITHOUT A SPECIFIC SUBJECT, INCLUDE MULTIPLE RELEVANT ENTITIES (Leads, Quotations, Invoices, Projects, Tasks).
                     - ""DETAIL"": Single record details (e.g., ""details of"", ""tell me more about"").
                   - FILTERS: 
                     - ""dateRange"": (e.g., ""last 7 days"", ""this month"", ""today"", ""yesterday"").
                     - ""assignedTo"": set to ""me"" if the user says ""my"".
                     - ""status"": (e.g., ""converted"", ""pending"", ""lost"").
                     - ""search"": specific names, ID strings, or search terms.
                   - IMPORTANT: ALL filter values MUST be simple strings, NOT objects.

                4. ""message"": Greeting or general talk.

                JSON FORMAT:
                {{
                  ""action"": ""create_lead"" | ""create_task"" | ""get_data"" | ""message"",
                  ""entities"": [""Leads"", ...],
                  ""type"": ""LIST"" | ""SUMMARY"" | ""DETAIL"",
                  ""filters"": {{ ""field"": ""value"" }},
                  ""parameters"": {{ ""field"": ""value"" }},
                  ""isClarificationRequired"": boolean,
                  ""clarificationMessage"": ""str"",
                  ""suggestions"": [""suggestion 1"", ""suggestion 2""]
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
