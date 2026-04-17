using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Domain.Constant;
using AvinyaAICRM.Shared.AI;
using AvinyaAICRM.Application.AI.Knowledge;
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

            // Use targeted schema if possible
            var targetedSchema = finalTables.Any() ? AISchema.GetTables(finalTables) : (isSuperAdmin ? AISchema.CRM : AISchema.GetTables(baseTables));
            bool isReportMode = lowerMessage.Contains("report") || lowerMessage.Contains("summary") || lowerMessage.Contains("overall");
            if (isReportMode) targetedSchema = AISchema.CRM;

            var securityRule = isSuperAdmin
                ? "1. SUPER ADMIN. Global access. Do NOT add TenantId filters unless specific."
                : "1. Per-tenant analyst.\n2. Use 'WHERE TenantId = @TenantId'.";

            var currentTimeContext = $"Current Date/Time: {DateTime.Now:f} (Year {DateTime.Now.Year})";

            var prompt = $@"
                You are a CRM assistant. Analyze input and return ONLY valid JSON.
                {currentTimeContext}

                {AIKnowledgeBase.GetFullContext()}

                TIME RULES: 
                - If the user specifies a date like ""15 April"", always assume the current year ({DateTime.Now.Year}) unless they say otherwise.
                - Use the provided Current Date/Time context for ALL relative time calculations.

                ACTIONS:
                1. ""create_lead"": Extract 'CompanyName', 'Mobile', 'Email', 'Notes'.
                2. ""create_task"": User wants to create a task.
                   - Extract: 'Title', 'Description', 'Notes', 'TaskScope' (Personal/Team), 'TeamName', 'AssignToName', 'DueDateTime', 'ReminderAt'.
                3. ""get_summary"": Generate a T-SQL SELECT query (Analytics/Reports).
                4. ""message"": General conversation.

                SQL RULES (MANDATORY):
                1. {securityRule}
                2. SECURITY: Include '@TenantId'. Ignore records with 'IsDeleted = 1'.
                3. JOIN LOGIC: Join on IDs, SELECT readable Names from Master tables.
                4. COLUMN NAMES: Clients table column is 'CompanyName'.
                5. ALIASES: Use readable column aliases like 'SELECT CompanyName AS [Client Name]...'.
                6. LIMITS: If the user asks for a specific count (e.g. ""give 5"", ""top 10"", ""last 3""), you MUST use 'SELECT TOP N ...' in your SQL.

                MESSAGE RULES (CRITICAL):
                - Always provide a conversational 'successMessage' and 'errorMessage'.
                - For 'get_summary': 
                  - successMessage: ""I've found [count] records for [Topic]. Here is the summary.""
                  - errorMessage: ""I couldn't find any data matching your request [Request Details]. Please try adjusting the filters.""
                - For 'create_lead' / 'create_task':
                  - successMessage: ""Great! I've prepared the [Entity] details for [Name]. Should I proceed with creating it?""
                - Use friendly, professional, and helpful language. Avoid technical jargon like ""SQL"" or ""Query"".

                REPORTING STRUCTURE (ONLY if 'report' or 'summary' is asked):
                Act like a BUSINESS ANALYST. Provide a summary message, breakdown, and insights. Use {{Value}} placeholders for dynamic data from the first row of result.

                JSON FORMAT (RETURN ONLY JSON):
                {{
                  ""action"": ""create_lead"" | ""create_task"" | ""get_summary"" | ""message"",
                  ""intent"": ""query_leads"" | ""query_revenue"" | ""report_summary"" | ""query_tasks"" | ""other"",
                  ""parameters"": {{ ... }},
                  ""sql"": ""SELECT ..."",
                  ""isClarificationRequired"": boolean,
                  ""clarificationMessage"": ""str"",
                  ""successMessage"": ""A friendly message when data is found"",
                  ""errorMessage"": ""A friendly message when no data is found or error occurs""
                }}

                Schema Context:
                {targetedSchema}

                User Input: {userMessage}";

            var geminiResult = await CallGeminiAsync(prompt, apiKey);
            if (string.IsNullOrEmpty(geminiResult.Text)) return new AIResponse { Action = "message", ErrorMessage = "AI service error." };

            try
            {
                var clean = CleanJsonResponse(geminiResult.Text);
                var response = JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();
                
                // Populate token usage
                response.PromptTokens = geminiResult.Prompt;
                response.ResponseTokens = geminiResult.Response;
                response.TotalTokens = geminiResult.Total;
                
                return response;
            }
            catch
            {
                return new AIResponse { Action = "message", ErrorMessage = "Error parsing AI response." };
            }
        }

        public async Task<AIResponse> RefineTemplateAsync(string userMessage, string templateSql, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var prompt = $@"
                You are a T-SQL expert. I have a base SQL template and a user request.
                
                USER REQUEST: ""{userMessage}""
                BASE TEMPLATE:
                ""{templateSql}""

                TASK:
                1. Refine the WHERE clause and JOINs of the BASE TEMPLATE to perfectly match the USER REQUEST.
                2. Keep the overall query structure exactly as it is (Columns, Group By, Order By).
                3. Apply TenantId = '{tenantId}' filtering if not already present.
                4. Do NOT change column names or table aliases from the template.
                5. RETURN ONLY A JSON OBJECT matching the schema below.

                TIME CONTEXT: Current Date/Time is {DateTime.Now:f} (Year {DateTime.Now.Year}).

                JSON SCHEMA:
                {{
                  ""action"": ""get_summary"",
                  ""sql"": ""The refined T-SQL query"",
                  ""successMessage"": ""A friendly message summarizing the specific filter applied (e.g. 'Pulling leads from the last 2 days only.')"",
                  ""errorMessage"": ""What to say if the request is impossible (e.g. 'I cannot filter by that specific category.')""
                }}
            ";

            var geminiResult = await CallGeminiAsync(prompt, apiKey);
            if (string.IsNullOrEmpty(geminiResult.Text)) return new AIResponse { Action = "get_summary", Sql = templateSql, SuccessMessage = "Proceeding with standard template." };

            try
            {
                var clean = CleanJsonResponse(geminiResult.Text);
                var response = JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();
                
                // Populate token usage
                response.PromptTokens = geminiResult.Prompt;
                response.ResponseTokens = geminiResult.Response;
                response.TotalTokens = geminiResult.Total;

                return response;
            }
            catch
            {
                return new AIResponse { Action = "get_summary", Sql = templateSql };
            }
        }

        public async Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var prompt = $@"
                You are a T-SQL expert. Fix the following SQL query.

                Original User Question: {originalQuestion}
                
                Broken SQL:
                {badSql}

                SQL Error:
                {errorMessage}

                Rules:
                - Always filter with TenantId = '{tenantId}' (or use @TenantId)
                - Only SELECT statements allowed
                - Fix ONLY the error, don't change the intent
                - Return ONLY the fixed SQL string, nothing else, no explanation.
                
                Schema:
                {AISchema.CRM}
            ";

            var geminiResult = await CallGeminiAsync(prompt, apiKey);
            return geminiResult.Text.Replace("```sql", "").Replace("```", "").Trim();
        }

        private async Task<(string Text, int Prompt, int Response, int Total)> CallGeminiAsync(string prompt, string apiKey)
        {
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            const int maxRetries = 3;
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.PostAsync(
                        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
                        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                    );

                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                        response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        if (i < maxRetries - 1)
                        {
                            await Task.Delay(1000 * (i + 1)); 
                            continue;
                        }
                    }

                    if (!response.IsSuccessStatusCode) return ("", 0, 0, 0);

                    var resultStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resultStr);
                    
                    var candidates = doc.RootElement.GetProperty("candidates");
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                    
                    int p = 0, r = 0, t = 0;
                    if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
                    {
                        // Check both camelCase and PascalCase
                        if (usage.TryGetProperty("promptTokenCount", out var pProp) || usage.TryGetProperty("promptTokens", out pProp)) 
                            p = pProp.GetInt32();
                        
                        if (usage.TryGetProperty("candidatesTokenCount", out var rProp) || usage.TryGetProperty("candidatesTokens", out rProp)) 
                            r = rProp.GetInt32();
                            
                        if (usage.TryGetProperty("totalTokenCount", out var tProp) || usage.TryGetProperty("totalTokens", out tProp)) 
                            t = tProp.GetInt32();
                    }

                    return (text, p, r, t);
                }
                catch
                {
                    if (i == maxRetries - 1) throw;
                    await Task.Delay(1000);
                }
            }
            return ("", 0, 0, 0);
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
