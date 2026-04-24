using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Domain.Constant;
using AvinyaAICRM.Shared.AI;
using AvinyaAICRM.Application.AI.Knowledge;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace AvinyaAICRM.Infrastructure.Repositories
{
    public class GroqService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public bool PreferRawGeneration => true;

        public GroqService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }
        
        private static List<string> DetectIntents(string msg)
        {
            var intents = new List<string>();

            // ── FOLLOWUPS ──────────
            if (msg.Contains("follow up") || msg.Contains("followup") ||
                msg.Contains("follow-up") || msg.Contains("upcoming followup") ||
                msg.Contains("overdue followup") || msg.Contains("pending followup") ||
                msg.Contains("completed followup") || msg.Contains("today's followup") ||
                msg.Contains("followups this week") || msg.Contains("followups this month") ||
                msg.Contains("what should i follow"))
                intents.Add("query_followups");

            // ── PAYMENTS & INVOICES ──────────
            if (msg.Contains("payment") || msg.Contains("paid") ||
                msg.Contains("cash payment") || msg.Contains("upi payment") ||
                msg.Contains("bank payment") || msg.Contains("payment received") ||
                msg.Contains("payment history") || msg.Contains("payment trend") ||
                msg.Contains("pending payment") || msg.Contains("collected amount") ||
                msg.Contains("total collected") || msg.Contains("invoice") || msg.Contains("billing") ||
                msg.Contains("outstanding") || msg.Contains("unpaid") ||
                msg.Contains("overdue invoice") || msg.Contains("partially paid") ||
                msg.Contains("revenue trend") || msg.Contains("invoice summary") ||
                msg.Contains("total invoice") || msg.Contains("invoice amount") || msg.Contains("revenue") || msg.Contains("profit") ||
                msg.Contains("sales") || msg.Contains("total collected") ||
                msg.Contains("revenue this month") || msg.Contains("revenue this year") ||
                msg.Contains("revenue vs expense") || msg.Contains("revenue trend") ||
                msg.Contains("how is my business") || msg.Contains("business doing") ||
                msg.Contains("growing") || msg.Contains("declining") ||
                msg.Contains("predict revenue") || msg.Contains("business overview") ||
                msg.Contains("performance report") || msg.Contains("summary report") ||
                msg.Contains("monthly report") || msg.Contains("yearly report") ||
                msg.Contains("business summary"))
                intents.Add("query_invoices");

            // ── EXPENSES ──────────
            if (msg.Contains("expense") || msg.Contains("spending") ||
                msg.Contains("travel expense") || msg.Contains("office expense") ||
                msg.Contains("food expense") || msg.Contains("utility") ||
                msg.Contains("total expense") || msg.Contains("highest expense") ||
                msg.Contains("expense trend") || msg.Contains("expense category") ||
                msg.Contains("where are we losing money") || msg.Contains("losing money"))
                intents.Add("query_expenses");

            // ── ORDERS ──────────
            if (msg.Contains("order") || msg.Contains("pending order") ||
                msg.Contains("delivered order") || msg.Contains("completed order") ||
                msg.Contains("in-progress order") || msg.Contains("inprogress order") ||
                msg.Contains("today's order") || msg.Contains("recent order") ||
                msg.Contains("order revenue") || msg.Contains("order status") ||
                msg.Contains("top order"))
                intents.Add("query_orders");

            // ── QUOTATIONS ──────────
            if (msg.Contains("quotation") || msg.Contains("quote") ||
                msg.Contains("accepted quotation") || msg.Contains("rejected quotation") ||
                msg.Contains("sent quotation") || msg.Contains("quotation sent"))
                intents.Add("query_quotations");

            // ── PROJECTS ──────────
            if (msg.Contains("project") || msg.Contains("active project") ||
                msg.Contains("completed project") || msg.Contains("pending project") ||
                msg.Contains("delayed project") || msg.Contains("project progress") ||
                msg.Contains("upcoming deadline") || msg.Contains("project manager") ||
                msg.Contains("project status"))
                intents.Add("query_projects");

            // ── TASKS ──────────
            if (msg.Contains("task") || msg.Contains("todo") ||
                msg.Contains("urgent task") || msg.Contains("pending task") ||
                msg.Contains("pending work") || msg.Contains("what is overdue") ||
                msg.Contains("needs attention") || msg.Contains("what needs attention") ||
                msg.Contains("urgent") || msg.Contains("overdue task"))
                intents.Add("query_tasks");

            // ── PRODUCTS ──────────
            if (msg.Contains("product") || msg.Contains("item") ||
                msg.Contains("low stock") || msg.Contains("product category") ||
                msg.Contains("active product"))
                intents.Add("query_products");

            // ── USERS ──────────
            if (msg.Contains("user") || msg.Contains("staff") ||
                msg.Contains("employee") || msg.Contains("team member") ||
                msg.Contains("role") || msg.Contains("top performer") ||
                msg.Contains("workload") || msg.Contains("performance of staff") ||
                msg.Contains("active user") || msg.Contains("all user"))
                intents.Add("query_users");

            // ── LEADS ──────────
            if (msg.Contains("assigned to me") || msg.Contains("which lead") ||
                msg.Contains("likely to convert") || msg.Contains("suggest which") ||
                msg.Contains("create lead") || msg.Contains("add lead") || msg.Contains("new lead registration"))
                intents.Add("create_lead");
            else if (msg.Contains("lead"))
                intents.Add("query_leads");

            // ── CLIENTS ──────────
            if (msg.Contains("client") || msg.Contains("customer") ||
                msg.Contains("company") || msg.Contains("inactive client") ||
                msg.Contains("top client") || msg.Contains("repeat client") || msg.Contains("highest revenue") ||
                msg.Contains("client report") || msg.Contains("full report of") ||
                msg.Contains("details of") || msg.Contains("activity for") ||
                msg.Contains("transactions for") || msg.Contains("interaction history") ||
                msg.Contains("last activity") || msg.Contains("growing client") ||
                msg.Contains("lost client") || msg.Contains("mobile number") ||
                msg.Contains("search client") || msg.Contains("find client"))
                intents.Add("query_clients");

            return intents;
        }

        public async Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isSuperAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null)
        {
            var apiKey  = _config["Groq:ApiKey"];
            var baseUrl = _config["Groq:BaseUrl"]  ?? "https://api.groq.com/openai/v1/chat/completions";

            var lowerMsg       = userMessage.ToLower();
            var intents        = DetectIntents(lowerMsg);
            var targetedSchema = AISchema.GetContextForIntent(intents);

            var historyContext = new StringBuilder();
            if (history != null && history.Any())
            {
                historyContext.AppendLine("RECENT CONVERSATION HISTORY (Last 5 messages):");
                foreach (var h in history.TakeLast(5))
                {
                    historyContext.AppendLine($"{h.Role.ToUpper()}: {h.Content}");
                }
            }

            // ─── DYNAMIC MODEL SELECTION (SPEED VS BRAIN) ────────────────────
            var isComplex = intents.Count > 1 || 
                            lowerMsg.Contains("revenue") || lowerMsg.Contains("trend") || 
                            lowerMsg.Contains("compare") || lowerMsg.Contains("profit") || 
                            lowerMsg.Contains("report") || lowerMsg.Contains("summary") || 
                            lowerMsg.Contains("how many") || lowerMsg.Contains("top ");
                            
            var model = isComplex ? "llama-3.3-70b-versatile" : "llama-3.1-8b-instant";

         var prompt = $@"
                You are a CRM Data Analyst and T-SQL Expert. Generate a SQL query based on the JSON context provided.

                GLOBAL RULES:
                - SECURITY      : Every single query MUST filter by 'TenantId = @TenantId'. This is MANDATORY.
                - SECURITY HINT : Check the 'hint' property in the table schema. If it says 'NO TenantId', YOU MUST JOIN a parent table that has it (e.g., JOIN Leads, Projects, AspNetUsers, or Teams) and filter by that table's TenantId.
                - INTEGRITY     : ONLY include 'IsDeleted = 0' IF the table explicitly has that column in the schema. (Note: TaskOccurrences and TaskSeries do NOT have IsDeleted, use IsActive for TaskSeries if relevant).
                - HALLUCINATION : Only use tables and columns defined in the JSON context.
                - LIMITS        : Use 'SELECT TOP 50' if the user doesn't specify a count.
                - TIME          : Current Date/Time is {DateTime.Now:f} (Year {DateTime.Now.Year}).
                - PARAMETERS    : You MUST use '@TenantId' parameter in every WHERE clause. NEVER hardcode a Guid or ID for TenantId.
                - USER FRIENDLY : NEVER SELECT raw internal IDs (like ClientID, StatusID) in the final output. ALWAYS JOIN the reference tables and SELECT the human-readable names (e.g. CompanyName, StatusName, SourceName).
                {historyContext}
                - DURATION vs COUNT: 
                    - IF question contains 'days', 'weeks', 'months', or 'years' → Use DATEADD filter.
                    - IF question contains ONLY a number (e.g., 'last 5', 'top 10') → Use SELECT TOP and ORDER BY Date DESC.
                    - CRITICAL: Never add DATEADD filters if the user did not say 'days/weeks/months'.

                KEYWORD HINTS (apply when the user question matches):
                - ""today""           → CAST(DateColumn AS DATE) = CAST(GETDATE() AS DATE)
                - ""this week""       → DATEPART(WEEK, DateColumn) = DATEPART(WEEK, GETDATE()) AND YEAR(DateColumn) = YEAR(GETDATE())
                - ""this month""      → MONTH(DateColumn) = MONTH(GETDATE()) AND YEAR(DateColumn) = YEAR(GETDATE())
                - ""last month""      → MONTH(DateColumn) = MONTH(DATEADD(MONTH,-1,GETDATE())) AND YEAR(DateColumn) = YEAR(DATEADD(MONTH,-1,GETDATE()))
                - ""this year""       → YEAR(DateColumn) = YEAR(GETDATE())
                - ""last X days""     → DateColumn >= DATEADD(DAY, -X, GETDATE())
                - ""last X [items]""  → SELECT TOP X ... ORDER BY DateColumn DESC
                - ""overdue""         → DateColumn < GETDATE()
                - ""upcoming""        → DateColumn >= GETDATE()
                - ""revenue""         → SUM(Invoices.GrandTotal)
                - ""outstanding""     → SUM(Invoices.OutstandingAmount)
                - ""collected""       → SUM(Payments.Amount)
                - ""top clients""     → GROUP BY + ORDER BY SUM DESC
                - ""by category""     → GROUP BY CategoryName
                - ""by status""       → GROUP BY StatusName
                - ""by source""       → GROUP BY SourceName
                - ""trend""           → GROUP BY MONTH(DateColumn), YEAR(DateColumn)
                - ""summary""         → aggregate query (COUNT, SUM)

                RULES:
                1. Only use DATEADD filters if the user says 'days', 'months', or 'weeks'.
                2. If they say 'last 5' or 'last 7' (WITHOUT the word 'days'), use 'SELECT TOP' with 'ORDER BY Date DESC'.
                3. NEVER combine SELECT TOP with DATEADD unless the user specifically asked for both.
                4. GLOBAL SEARCH: If the user provides a search term (name, ID, or reference) without specifying a column, you MUST search for that term across all relevant human-readable columns (CompanyName, ContactPerson, LeadNo, OrderNo, etc.) using LIKE and OR. Do NOT just check one specific ID column.
                5. ANTI-HALLUCINATION: NEVER invent table names (like 'LeadNotes' or 'LeadItems'). ONLY use the tables provided in the JSON context. Notes for Leads are located in `Leads.Notes` or `Leads.RequirementDetails`.
                6. DYNAMIC SUGGESTIONS: You MUST always provide 3-5 highly relevant, short follow-up questions or actions in the ""suggestions"" array.

                DATABASE CONTEXT (JSON):
                {targetedSchema}

                OUTPUT FORMAT (STRICT JSON — no extra text outside this object):
                {{
                ""action"": ""[get_summary | create_lead | create_task | message]"",
                ""intent"": ""[query_leads | create_lead | create_task | general_chat | ...]"",
                ""sql"": ""[YOUR_SINGLE_LINE_SQL_HERE (only if action is get_summary)]"",
                ""parameters"": {{ 
                    // --- LEAD CREATION FIELDS (Use only for create_lead) ---
                    ""CompanyName"": ""(Required) The name of the company/client"",
                    ""RequirementDetails"": ""(Required) What the lead needs/requires"",
                    ""ContactPerson"": ""Name of the contact person"",
                    ""Mobile"": ""Contact mobile number"",
                    ""Email"": ""Contact email address"",
                    ""ClientType"": ""[Individual | Company] (Individual if personal lead)"",
                    ""GSTNo"": ""GST number of the company"",
                    ""BillingAddress"": ""Full billing address"",
                    ""StateID"": ""State name (resolved by backend)"",
                    ""CityID"": ""City name (resolved by backend)"",
                    ""LeadSourceID"": ""Source name, e.g. 'WhatsApp'"",
                    ""LeadStatusID"": ""Status name, e.g. 'Hot'"",
                    ""NextFollowupDate"": ""Date and time for the next followup"",
                    ""OtherSources"": ""Any other source info"",
                    ""Links"": ""Relevant social/web links"",

                    // --- TASK CREATION FIELDS (Use only for create_task) ---
                    ""Title"": ""(Required) Short summary of the task"",
                    ""Description"": ""Detailed description of the task"",
                    ""DueDateTime"": ""(Required) Date and time when task is due"",
                    ""ListName"": ""Name of the task list to add to (e.g. 'Work', 'General')"",
                    ""TaskType"": ""[Personal | Team] (Required) Who is this for?"",
                    ""TeamName"": ""(Required if TaskType is Team) Name of the team"",
                    ""AssignToName"": ""Full name of the person to assign to"",
                    ""ProjectName"": ""Name of the project this task belongs to"",
                    ""ReminderAt"": ""DateTime for a reminder notification"",
                    ""IsRecurring"": ""[true | false] If the task repeats"",
                    ""RecurrenceRule"": ""RRULE string if recurring (e.g. 'FREQ=DAILY')"",
                    
                    // --- SHARED / MISC FIELDS ---
                    ""Notes"": ""Additional internal notes"",
                    ""AssignedTo"": ""Alias for AssignToName (Full Name)""
                }},
                ""successMessage"": ""Write a highly conversational, engaging business reply. Use {{ColumnName}} syntax for data placeholders (e.g., 'I found {{TotalLeads}} leads.') and ALWAYS use {{count}} to represent the total number of records returned (e.g., 'I found {{count}} leads for you.')."",
                ""errorMessage"": ""Use this ONLY to ask for MISSING REQUIRED FIELDS or explain errors. Keep it brief."",
                ""suggestions"": [""Next logical question 1"", ""Next logical question 2"", ""Next logical question 3""]
                }}
 
                ACTION SELECTION RULES:
                1. If the user wants to ADD, CREATE, or REGISTER a lead:
                   - IF CompanyName and RequirementDetails are provided → action: ""create_lead"".
                   - IF either is missing → action: ""message"" and ask the user for the missing info.
                2. If the user wants to ADD, CREATE, or SCHEDULE a task/todo:
                   - IF Title, DueDateTime, and TaskType are provided → action: ""create_task"".
                   - IF any are missing → action: ""message"" and ask the user for the missing info.
                3. If the user asks a question that requires data from the database → action: ""get_summary"".
                4. If the user is just saying hello or asking a general question → action: ""message"".

                ACTION 'create_lead' RULES:
                - ALWAYS extract CompanyName and RequirementDetails.
                - IF the user has NOT provided them yet, YOU MUST set `action: ""message""` and ask: ""Sure, I can help with that. What is the Company Name and their Requirement?""
                - NEVER use ""Required"" or ""Missing"" as a value in parameters. Use actual user data only.

                ACTION 'create_task' RULES:
                - Extract Title, DueDateTime, and TaskType.
                - If the user says ""team task"", ""task for the team"", or mentions a team → TaskType: ""Team"".
                - IF TaskType is not mentioned, set `errorMessage` to ask: ""Is this for personal use or for a team?"".
                - IF TaskType is Team and TeamName is missing, set `errorMessage` to ask: ""Which team should I assign this to?"".
                - IF DueDateTime is missing, set `errorMessage` to ask: ""When is this task due?"".

                USER QUESTION: {userMessage}

                Generate ONLY the JSON object. No explanation, no markdown.
                ";

            var groqResult = await CallGroqAsync(prompt, apiKey, model, baseUrl);
            
            // --- FALLBACK MECHANISM ---
            if (string.IsNullOrEmpty(groqResult.Text))
            {
                // If the primary model failed, try the alternative
                var fallbackModel = isComplex ? "llama-3.1-8b-instant" : "llama-3.3-70b-versatile";
                groqResult = await CallGroqAsync(prompt, apiKey, fallbackModel, baseUrl);
            }

            if (string.IsNullOrEmpty(groqResult.Text))
                return new AIResponse { Action = "message", ErrorMessage = "AI service error (Groq)." };

            try
            {
                var clean    = CleanJsonResponse(groqResult.Text);
                var response = JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();

                response.Parameters ??= new(StringComparer.OrdinalIgnoreCase);
                response.PromptTokens    = groqResult.Prompt;
                response.ResponseTokens  = groqResult.Response;
                response.TotalTokens     = groqResult.Total;

                return response;
            }
            catch
            {
                return new AIResponse { Action = "message", ErrorMessage = "Error parsing Groq response." };
            }
        }

        public async Task<AIResponse> RefineTemplateAsync(string userMessage, string templateSql, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey  = _config["Groq:ApiKey"];
            var model   = _config["Groq:Model"]   ?? "llama-3.1-8b-instant";
            var baseUrl = _config["Groq:BaseUrl"]  ?? "https://api.groq.com/openai/v1/chat/completions";

            var prompt = $@"
                        You are a T-SQL expert. Refine the BASE TEMPLATE to match the USER REQUEST exactly.

                        USER REQUEST: ""{userMessage}""

                        BASE TEMPLATE:
                        ""{templateSql}""

                        RULES:
                        1. Only modify the WHERE clause and JOINs to match the user request.
                        2. Keep all columns, aliases, GROUP BY, ORDER BY exactly as in the template.
                        3. Apply TenantId = '{tenantId}' filtering if not already present.
                        4. Do NOT invent new columns or tables.
                        5. Return ONLY the JSON object below — no extra text.

                        TIME CONTEXT: Current Date/Time is {DateTime.Now:f} (Year {DateTime.Now.Year}).

                        TIME HINTS:
                        - ""today""       → CAST(DateColumn AS DATE) = CAST(GETDATE() AS DATE)
                        - ""this week""   → DATEPART(WEEK, DateColumn) = DATEPART(WEEK, GETDATE())
                        - ""this month""  → MONTH(DateColumn) = MONTH(GETDATE()) AND YEAR(DateColumn) = YEAR(GETDATE())
                        - ""last month""  → MONTH(DateColumn) = MONTH(DATEADD(MONTH,-1,GETDATE()))
                        - ""last 7 days"" → DateColumn >= DATEADD(DAY,-7,GETDATE())
                        - ""overdue""     → DateColumn < GETDATE()

                        JSON SCHEMA:
                        {{
                        ""action"": ""get_summary"",
                        ""sql"": ""The refined T-SQL query (single line)"",
                        ""successMessage"": ""A friendly 1-line message summarizing the filter applied (e.g. 'Pulling leads from the last 2 days.')"",
                        ""errorMessage"": ""What to say if the request cannot be fulfilled (or empty string if OK)""
                        }}
                        ";

            var groqResult = await CallGroqAsync(prompt, apiKey, model, baseUrl);

            // --- FALLBACK ---
            if (string.IsNullOrEmpty(groqResult.Text))
            {
                var fallbackModel = (model == "llama-3.3-70b-versatile") ? "llama-3.1-8b-instant" : "llama-3.3-70b-versatile";
                groqResult = await CallGroqAsync(prompt, apiKey, fallbackModel, baseUrl);
            }

            if (string.IsNullOrEmpty(groqResult.Text))
                return new AIResponse { Action = "get_summary", Sql = templateSql, SuccessMessage = "Proceeding with standard template." };

            try
            {
                var clean    = CleanJsonResponse(groqResult.Text);
                var response = JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();

                response.PromptTokens   = groqResult.Prompt;
                response.ResponseTokens = groqResult.Response;
                response.TotalTokens    = groqResult.Total;

                return response;
            }
            catch
            {
                return new AIResponse { Action = "get_summary", Sql = templateSql };
            }
        }

   

        public async Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey  = _config["Groq:ApiKey"];
            var model   = _config["Groq:Model"]   ?? "llama-3.1-8b-instant";
            var baseUrl = _config["Groq:BaseUrl"]  ?? "https://api.groq.com/openai/v1/chat/completions";

            var fixSchema = AISchema.GetContextForIntent(DetectIntents(originalQuestion.ToLower()));

            var prompt = $@"
                        You are a T-SQL expert. Fix the broken SQL query below so it runs without errors.

                        ORIGINAL USER QUESTION: {originalQuestion}

                        BROKEN SQL:
                        {badSql}

                        SQL ERROR:
                        {errorMessage}

                        FIX RULES:
                        - Always filter with TenantId = @TenantId. If the table lacks TenantId, JOIN a parent table that has it (Leads, Projects, AspNetUsers, etc.).
                        - Only SELECT statements are allowed
                        - Fix ONLY the error — do not change the intent or columns
                        - Return ONLY the fixed SQL string. No explanation, no JSON, no markdown.

                        SCHEMA CONTEXT (JSON):
                        {fixSchema}
                        ";

            var groqResult = await CallGroqAsync(prompt, apiKey, model, baseUrl);
            return groqResult.Text.Replace("```sql", "").Replace("```", "").Trim();
        }

        public async Task<AIResponse> RefineQueryAsync(string originalMessage, string badSql, string userCorrection, Guid tenantId)
        {
            var groqKey = _config["Groq:ApiKey"];
            var model   = _config["Groq:Model"]   ?? "llama-3.1-8b-instant";
            var baseUrl = _config["Groq:BaseUrl"]  ?? "https://api.groq.com/openai/v1/chat/completions";
            
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
                    (SELECT TOP 10 l.LeadNo, c.CompanyName, ls.StatusName, CONVERT(varchar(10), l.CreatedDate, 120) AS Date FROM dbo.Leads l JOIN dbo.Clients c ON l.ClientID = c.ClientID JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID WHERE l.TenantId = @TenantId AND l.IsDeleted = 0 ORDER BY l.CreatedDate DESC FOR JSON PATH) AS RecentLeads,
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

            var result = await CallGroqAsync(prompt, groqKey, model, baseUrl);

            // --- FALLBACK ---
            if (string.IsNullOrEmpty(result.Text))
            {
                var fallbackModel = (model == "llama-3.3-70b-versatile") ? "llama-3.1-8b-instant" : "llama-3.3-70b-versatile";
                result = await CallGroqAsync(prompt, groqKey, fallbackModel, baseUrl);
            }

            if (string.IsNullOrEmpty(result.Text))
                return new AIResponse { Action = "message", ErrorMessage = "Healing AI failed." };
                
            return new AIResponse 
            { 
                Sql = result.Text.Replace("```sql", "").Replace("```", "").Trim(),
                TotalTokens = result.Total,
                PromptTokens = result.Prompt,
                ResponseTokens = result.Response
            };
        }

        private async Task<(string Text, int Prompt, int Response, int Total)> CallGroqAsync(
            string prompt, string apiKey, string model, string baseUrl)
        {
            var requestBody = new
            {
                model    = model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await _httpClient.PostAsync(
                    baseUrl,
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode) return ("", 0, 0, 0);

                var resultStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resultStr);

                var choices = doc.RootElement.GetProperty("choices");
                var text    = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                int p = 0, r = 0, t = 0;
                if (doc.RootElement.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens",      out var pProp)) p = pProp.GetInt32();
                    if (usage.TryGetProperty("completion_tokens",  out var rProp)) r = rProp.GetInt32();
                    if (usage.TryGetProperty("total_tokens",       out var tProp)) t = tProp.GetInt32();
                }

                return (text, p, r, t);
            }
            catch
            {
                return ("", 0, 0, 0);
            }
        }

        private static string CleanJsonResponse(string text)
        {
            var start = text.IndexOf("{");
            var end   = text.LastIndexOf("}") + 1;
            if (start == -1 || end <= 0) return "{}";

            var json = text.Substring(start, end - start);
            json = json.Replace("\r\n", " ").Replace("\n", " ");
            return json;
        }
    }
}