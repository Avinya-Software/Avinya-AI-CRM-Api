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
        public bool PreferRawGeneration => false;

        public GeminiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isSuperAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var lowerMessage = userMessage.ToLower();

            var intents        = DetectIntents(lowerMessage);
            var targetedSchema = AISchema.GetContextForIntent(intents);

            var historyContext = new StringBuilder();
            if (history != null && history.Any())
            {
                historyContext.AppendLine("RECENT CONVERSATION HISTORY:");
                foreach (var h in history.TakeLast(5))
                {
                    historyContext.AppendLine($"{h.Role.ToUpper()}: {h.Content}");
                }
            }

            var systemPrompt = $@"
                You are a CRM Data Analyst and T-SQL Expert.
                RULES:
                1. SECURITY: You MUST use '@TenantId' parameter in every WHERE clause. NEVER hardcode a Guid or ID.
                2. INTEGRITY: Always include 'IsDeleted = 0' for every table involved.
                3. LIMITS: Use 'SELECT TOP 50' unless specified.
                4. FORMAT: Return ONLY a valid JSON object. No markdown, no explanation.
                {historyContext}
                USER QUESTION: {userMessage}
                RULES:
                1. Only use DATEADD filters if the user says 'days', 'months', or 'weeks'.
                2. If they say 'last 5' or 'last 7' (WITHOUT the word 'days'), use 'SELECT TOP' with 'ORDER BY Date DESC'.
                3. NEVER combine SELECT TOP with DATEADD unless the user specifically asked for both.
                4. GLOBAL SEARCH: If the user provides a search term (name, ID, or reference) without specifying a column, you MUST search for that term across all relevant human-readable columns (CompanyName, ContactPerson, LeadNo, OrderNo, etc.) using LIKE and OR. Do NOT just check one specific ID column.
                5. ANTI-HALLUCINATION: NEVER invent table names (like 'LeadNotes' or 'LeadItems'). ONLY use the tables provided in the JSON context. Notes for Leads are located in `Leads.Notes` or `Leads.RequirementDetails`.

                DATABASE CONTEXT (JSON): NEVER invent table names (like 'LeadNotes' or 'LeadItems'). ONLY use the tables provided in the JSON context. Notes for Leads are located in `Leads.Notes` or `Leads.RequirementDetails`.
                OUTPUT FORMAT (STRICT JSON — no extra text outside this object):
                {{
                ""action"": ""[get_summary | create_lead | create_task]"",
                ""sql"": ""[YOUR_SINGLE_LINE_SQL_HERE (only if action is get_summary)]"",
                ""parameters"": {{ ""CompanyName"": ""..."", ""ContactPerson"": ""..."", ""Description"": ""..."" }},
                ""successMessage"": ""Write a highly conversational, engaging business reply. DO NOT use words like 'database', 'records', or 'I found results'. Example: 'Here is a quick snapshot of your business performance!' or 'I've pulled up the revenue details you asked for.'"",
                ""errorMessage"": """"
                }}
            ";

            var userPrompt = $@"
                DATABASE CONTEXT (JSON): {targetedSchema}
                USER QUESTION: {userMessage}
            ";
            var prompt = $"{systemPrompt}\n\n{userPrompt}";
            var geminiResult = await CallGeminiAsync(prompt, apiKey);
            
            if (string.IsNullOrEmpty(geminiResult.Text)) return new AIResponse { Action = "message", ErrorMessage = "AI service error (Gemini)." };

            try {
                var clean = CleanJsonResponse(geminiResult.Text);
                return JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();
            } catch {
                return new AIResponse { Action = "message", ErrorMessage = "Error parsing Gemini response." };
            }
        }

        public async Task<AIResponse> RefineTemplateAsync(string userMessage, string templateSql, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var prompt = $@"
                You are a CRM SQL Expert.
                Modify this SQL template to answer the user's specific question.
                
                USER QUESTION: {userMessage}
                
                TEMPLATE SQL: 
                {templateSql}
                
                RULES:
                - Do not change the core joins unless absolutely necessary.
                - Keep the TenantId filter intact.
                - Add/modify WHERE clauses or ORDER BY based on the user's question.
                - Return ONLY the modified SQL string. No explanation, no JSON, no markdown.
            ";

            var geminiResult = await CallGeminiAsync(prompt, apiKey);
            var sql = geminiResult.Text.Replace("```sql", "").Replace("```", "").Trim();
            
            return new AIResponse { Action = "get_summary", Sql = sql };
        }

        public async Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var fixSchema = AISchema.GetContextForIntent(DetectIntents(originalQuestion.ToLower()));

            var prompt = $@"
                Fix this T-SQL error.
                Error: {errorMessage}
                SQL: {badSql}
                Context: {fixSchema}
                Return ONLY the fixed SQL string.
            ";

            var geminiResult = await CallGeminiAsync(prompt, apiKey);
            return geminiResult.Text.Replace("```sql", "").Replace("```", "").Trim();
        }

        public async Task<AIResponse> RefineQueryAsync(string originalMessage, string badSql, string userCorrection, Guid tenantId)
        {
            var apiKey = _config["Gemini:ApiKey"];
            
            var intents = DetectIntents(originalMessage.ToLower());
            var schema  = AISchema.GetContextForIntent(intents);
            
            var prompt = $@"
                You are a CRM SQL Expert.
                A user previously asked: '{originalMessage}'
                You generated this SQL: {badSql}
                
                The user says this is wrong because: '{userCorrection}'
                
                DATABASE SCHEMA CONTEXT:
                {schema}

                SPECIAL TEMPLATE - Universal Business Summary (Use if user wants 'all modules', 'full report', or 'business overview'):
                SELECT 
                    (SELECT COUNT(*) FROM dbo.Clients WHERE TenantId = @TenantId AND IsDeleted = 0) AS ClientsCount,
                    (SELECT COUNT(*) FROM dbo.Leads WHERE TenantId = @TenantId AND IsDeleted = 0) AS LeadsCount,
                    (SELECT COUNT(*) FROM dbo.Orders WHERE TenantId = @TenantId AND IsDeleted = 0) AS OrdersCount,
                    (SELECT '₹ ' + FORMAT(ISNULL(SUM(GrandTotal), 0), 'N2') FROM dbo.Invoices WHERE TenantId = @TenantId AND IsDeleted = 0) AS TotalRevenue,
                    (SELECT '₹ ' + FORMAT(ISNULL(SUM(Amount), 0), 'N2') FROM dbo.Expenses WHERE TenantId = @TenantId AND IsDeleted = 0) AS TotalExpenses,
                    (SELECT TOP 10 l.LeadNo, c.CompanyName, ls.StatusName, CONVERT(varchar(10), l.Date, 120) AS Date FROM dbo.Leads l JOIN dbo.Clients c ON l.ClientID = c.ClientID JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID WHERE l.TenantId = @TenantId AND l.IsDeleted = 0 ORDER BY l.Date DESC FOR JSON PATH) AS RecentLeads,
                    (SELECT TOP 10 o.OrderNo, c.CompanyName, '₹ ' + FORMAT(o.GrandTotal, 'N2') AS Amount, osm.StatusName AS Status FROM dbo.Orders o JOIN dbo.Clients c ON o.ClientID = c.ClientID JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID WHERE o.TenantId = @TenantId AND o.IsDeleted = 0 ORDER BY o.OrderDate DESC FOR JSON PATH) AS RecentOrders,
                    (SELECT TOP 10 t.Title, p.ProjectName, t.Priority, t.StartDate FROM dbo.TaskSeries t JOIN dbo.Projects p ON t.ProjectId = p.ProjectID WHERE p.TenantId = @TenantId AND p.IsDeleted = 0 AND t.IsActive = 1 ORDER BY t.StartDate DESC FOR JSON PATH) AS RecentTasks
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER

                Fix the SQL to incorporate the user's feedback. 
                RULES:
                1. Include 'IsDeleted = 0' and '@TenantId' ONLY if those columns exist in the provided SCHEMA for the table.
                2. If a table lacks security columns (like TaskSeries), JOIN it with a related table that has them (like Projects).
                3. ONLY use tables and columns from the SCHEMA CONTEXT provided.
                4. If the user asks for 'all modules', 'full detail', or 'business summary', use the FOR JSON PATH format shown in the SPECIAL TEMPLATE.
                5. Return ONLY the corrected SQL string. No markdown, no explanation.
            ";

            var result = await CallGeminiAsync(prompt, apiKey);
            return new AIResponse {
                Sql = result.Text.Replace("```sql", "").Replace("```", "").Trim(),
                TotalTokens = result.Total,
                PromptTokens = result.Prompt,
                ResponseTokens = result.Response
            };
        }

        private static List<string> DetectIntents(string msg)
        {
            var intents = new List<string>();
            if (msg.Contains("follow"))  intents.Add("query_followups");
            if (msg.Contains("lead"))    intents.Add("query_leads");
            if (msg.Contains("order"))   intents.Add("query_orders");
            if (msg.Contains("expense") || msg.Contains("spending") || msg.Contains("loss")) intents.Add("query_expenses");
            if (msg.Contains("invoice") || msg.Contains("payment") || msg.Contains("revenue") || msg.Contains("profit") || msg.Contains("business")) intents.Add("query_invoices");
            if (msg.Contains("client")  || msg.Contains("customer")) intents.Add("query_clients");

            return intents;
        }

        private async Task<(string Text, int Prompt, int Response, int Total)> CallGeminiAsync(string prompt, string apiKey)
        {
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            try
            {
                var response = await _httpClient.PostAsync(
                    "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode) return ("", 0, 0, 0);

                var resultStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resultStr);
                
                var candidates = doc.RootElement.GetProperty("candidates");
                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                
                int promptTokens = 0;
                int candidateTokens = 0;
                int totalTokens = 0;

                if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
                {
                    usage.TryGetProperty("promptTokenCount", out var p);
                    usage.TryGetProperty("candidatesTokenCount", out var c);
                    usage.TryGetProperty("totalTokenCount", out var t);

                    promptTokens = p.GetInt32();
                    candidateTokens = c.GetInt32();
                    totalTokens = t.GetInt32();
                }

                return (text, promptTokens, candidateTokens, totalTokens);
            }
            catch
            {
                return ("", 0, 0, 0);
            }
        }

        private string CleanJsonResponse(string text)
        {
            text = text.Replace("`json", "").Replace("`", "").Trim();
            var start = text.IndexOf("{");
            var end = text.LastIndexOf("}") + 1;
            if (start == -1 || end == -1) return "{}";
            return text.Substring(start, end - start);
        }
    }
}
