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
        private const string DefaultModel = "meta-llama/llama-4-scout-17b-16e-instruct";
        private const string SmartModel = "llama-3.3-70b-versatile";
        private const string DefaultBaseUrl = "https://api.groq.com/openai/v1/chat/completions";

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
            if (msg.Contains("add expense") || msg.Contains("create expense") ||
                msg.Contains("record expense") || msg.Contains("save expense") ||
                msg.Contains("spent") || msg.Contains("spending") || 
                msg.Contains("payment of") || msg.Contains("bill for"))
                intents.Add("create_expense");
            else if (msg.Contains("expense") || msg.Contains("spending") ||
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
            if (msg.Contains("add task") || msg.Contains("create task") ||
                msg.Contains("schedule task") || msg.Contains("add todo") ||
                msg.Contains("create todo"))
                intents.Add("create_task");

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
            var apiKey = _config["Groq:ApiKey"];
            var baseUrl = GetBaseUrl();

            var lowerMsg       = userMessage.ToLower();
            var intents        = DetectIntents(lowerMsg);
            var targetedSchema = AISchema.GetContextForIntent(intents, allowedModules, isSuperAdmin);
            var allowedChatActions = ResolveAllowedChatActions(allowedModules, isSuperAdmin);
            var allowedActionText = allowedChatActions.Count == 0
                ? "none"
                : string.Join(", ", allowedChatActions);
            var historyContext = BuildHistoryContext(history);

            // ─── DYNAMIC MODEL SELECTION (SPEED VS BRAIN) ────────────────────
            var isComplex = intents.Count > 1 || 
                            lowerMsg.Contains("revenue") || lowerMsg.Contains("trend") || 
                            lowerMsg.Contains("compare") || lowerMsg.Contains("profit") || 
                            lowerMsg.Contains("report") || lowerMsg.Contains("summary") || 
                            lowerMsg.Contains("how many") || lowerMsg.Contains("top ");
                            
            var model = isComplex ? SmartModel : DefaultModel;

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
                - PERMISSIONS   : The JSON context contains ONLY the tables this user can access. If the requested module/table is missing, return action ""message"", empty sql, and explain that the user does not have permission for that data.
                - ACTION ACCESS : Allowed write actions for this user: {allowedActionText}. Never choose a create_* action that is not listed here.
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
                - ""outstanding""     → SUM(Invoices.AmountAfterDiscount)
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
                
                CRITICAL NEGATIVE RULES:
                - If action is 'create_lead', 'create_task', or 'create_expense', you MUST set ""sql"": """" (empty string).
                - NEVER generate INSERT, UPDATE, or DELETE SQL statements. The system only allows SELECT.

                OUTPUT FORMAT (STRICT JSON — no extra text outside this object):
                {{
                ""action"": ""[get_summary | create_lead | create_task | create_expense | message]"",
                ""intent"": ""[query_leads | create_lead | create_task | create_expense | general_chat | ...]"",
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

                    // --- EXPENSE CREATION FIELDS (Use only for create_expense) ---
                    ""Amount"": ""(Required) The numeric amount spent"",
                    ""ExpenseDate"": ""(Required) Date of the expense (default to today)"",
                    ""CategoryName"": ""(Required) Category like Office, Travel, Food, etc."",
                    ""PaymentMode"": ""Mode like Cash, UPI, Card, etc."",
                    
                    // --- SHARED / MISC FIELDS ---
                    ""Notes"": ""Additional internal notes"",
                    ""AssignedTo"": ""Alias for AssignToName (Full Name)""
                }},
                ""successMessage"": ""Write a highly conversational, engaging business reply. 
                    - For 'create_lead' or 'create_task', use {{FieldName}} syntax for parameters (e.g., 'I created a lead for {{CompanyName}}'). 
                    - For 'get_summary', ALWAYS use {{count}} for the total record count (e.g., 'I found {{count}} leads.').
                    - IMPORTANT: Do NOT mention counts for 'create_lead' or 'create_task' unless explicitly relevant."",
                ""errorMessage"": ""Use this ONLY to ask for MISSING REQUIRED FIELDS or explain errors. Keep it brief."",
                ""suggestions"": [""Next logical question 1"", ""Next logical question 2"", ""Next logical question 3""]
                }}
 
                ACTION SELECTION RULES:
                1. If the user wants to ADD, CREATE, or REGISTER a lead:
                   - Only choose ""create_lead"" when it is listed in Allowed write actions.
                   - IF CompanyName and RequirementDetails are provided → action: ""create_lead"".
                   - IF either is missing → action: ""message"" and ask the user for the missing info.
                2. If the user wants to ADD, CREATE, or SCHEDULE a task/todo:
                   - Only choose ""create_task"" when it is listed in Allowed write actions.
                   - IF Title, DueDateTime, and TaskType are provided → action: ""create_task"".
                   - IF any are missing → action: ""message"" and ask the user for the missing info.
                3. If the user wants to ADD, RECORD, or SAVE an expense:
                   - Only choose ""create_expense"" when it is listed in Allowed write actions.
                   - IF Amount and CategoryName are provided → action: ""create_expense"".
                   - IF either is missing → action: ""message"" and ask for missing info.
                4. If the user asks a question that requires data from the database → action: ""get_summary"".
                5. If the user is just saying hello or asking a general question → action: ""message"".

                ACTION 'create_expense' RULES:
                - ALWAYS extract Amount and CategoryName.
                - Extract ExpenseDate if mentioned, otherwise use today.
                - If the user mentions a payment method (Cash, Bank, GPay, etc.), extract into PaymentMode.
                - If the user provides a receipt or file, AI should assume this is an expense intent.

                ACTION 'create_lead' RULES:
                - ALWAYS extract CompanyName and RequirementDetails.
                - IF the user mentions a source like 'Referral', 'Facebook', 'Other', etc., extract it into `OtherSources`.
                - IF the user has NOT provided CompanyName and RequirementDetails yet, YOU MUST set `action: ""message""` and ask for them.
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

            var groqResult = await CallGroqWithFallbackAsync(prompt, apiKey, model, GetFallbackModel(model), baseUrl);

            if (string.IsNullOrEmpty(groqResult.Text))
                return new AIResponse { Action = "message", ErrorMessage = "AI service error (Groq)." };

            try
            {
                var clean = CleanJsonResponse(groqResult.Text);
                try 
                {
                    var response = JsonSerializer.Deserialize<AIResponse>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();
                    response.Parameters ??= new(StringComparer.OrdinalIgnoreCase);
                    response.PromptTokens    = groqResult.Prompt;
                    response.ResponseTokens  = groqResult.Response;
                    response.TotalTokens     = groqResult.Total;
                    return response;
                }
                catch
                {
                    // Fallback: If standard clean failed, try finding the LAST JSON block in the text
                    // This is useful if the AI rambles before or after the JSON.
                    var lastJson = ExtractLastJsonObject(groqResult.Text);
                    var response = JsonSerializer.Deserialize<AIResponse>(lastJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AIResponse();
                    response.Parameters ??= new(StringComparer.OrdinalIgnoreCase);
                    response.PromptTokens    = groqResult.Prompt;
                    response.ResponseTokens  = groqResult.Response;
                    response.TotalTokens     = groqResult.Total;
                    return response;
                }
            }
            catch
            {
                return new AIResponse { Action = "message", ErrorMessage = "Error parsing Groq response." };
            }
        }

        public async Task<AIResponse> RefineTemplateAsync(string userMessage, string templateSql, Guid tenantId, bool isSuperAdmin)
        {
            var apiKey = _config["Groq:ApiKey"];
            var model = GetConfiguredModel();
            var baseUrl = GetBaseUrl();

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

            var groqResult = await CallGroqWithFallbackAsync(prompt, apiKey, model, GetFallbackModel(model), baseUrl);

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

   

        public async Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, string userId, bool isSuperAdmin, List<AIChatHistoryDto> history = null, List<string> allowedModules = null)
        {
            var apiKey = _config["Groq:ApiKey"];
            var model = GetConfiguredModel();
            var baseUrl = GetBaseUrl();

            var intents = DetectIntents(originalQuestion.ToLower());
            var fixSchema = AISchema.GetContextForIntent(intents, allowedModules, isSuperAdmin);
            var historyContext = BuildHistoryContext(history);

            var prompt = $@"
                        You are a T-SQL expert. Fix the broken SQL query below so it runs without errors.
                        
                        TENANT CONTEXT:
                        - Current TenantId: {tenantId}
                        - Current UserId: {userId}

                        ORIGINAL USER QUESTION: {originalQuestion}

                        BROKEN SQL:
                        {badSql}

                        SQL ERROR / VALIDATION FAILURE:
                        {errorMessage}

                        {(string.IsNullOrEmpty(historyContext) ? "" : $"CONVERSATION CONTEXT (why the user asked this):\n{historyContext}")}

                        FIX RULES:
                        - Always filter with TenantId = @TenantId. If the table lacks TenantId, JOIN a parent table that has it (Leads, Projects, AspNetUsers, etc.).
                        - Only SELECT statements are allowed.
                        - Fix ONLY the error — do not change the intent or columns.
                        - Use the SCHEMA CONTEXT below to find the correct table/column names.
                        - Do not add tables that are not present in the SCHEMA CONTEXT.
                        - Return ONLY the fixed SQL string. No explanation, no JSON, no markdown.

                        SCHEMA CONTEXT (JSON):
                        {fixSchema}
                        ";

            var groqResult = await CallGroqWithFallbackAsync(prompt, apiKey, model, GetFallbackModel(model), baseUrl);
            return groqResult.Text.Replace("```sql", "").Replace("```", "").Trim();
        }


        public async Task<AIResponse> RefineQueryAsync(string originalMessage, string badSql, string userCorrection, Guid tenantId, string userId, bool isSuperAdmin = false, List<string> allowedModules = null)
        {
            var groqKey = _config["Groq:ApiKey"];
            var model = GetConfiguredModel();
            var baseUrl = GetBaseUrl();
            
            var intents = DetectIntents(originalMessage.ToLower());
            var schema  = AISchema.GetContextForIntent(intents, allowedModules, isSuperAdmin);

            var prompt = $@"
                You are a CRM SQL Expert.
                A user previously asked: '{originalMessage}'
                You generated this SQL: {badSql}
                
                The user says this is wrong because: '{userCorrection}'
                
                TENANT CONTEXT:
                - Current TenantId: {tenantId}
                - Current UserId: {userId}

                DATABASE SCHEMA CONTEXT:
                {schema}

                PERMISSION NOTE: For broad reports, only use tables present in DATABASE SCHEMA CONTEXT. Do not use older universal summary templates.
                DO NOT use this disabled legacy template:
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
                1. SECURITY MANDATE: You MUST include 'TenantId = @TenantId' in the WHERE clause if the table supports it. NEVER remove this filter even if the user correction doesn't mention it.
                2. INTEGRITY: Include 'IsDeleted = 0' ONLY if that column exists in the provided SCHEMA for the table.
                3. JOIN SECURITY: If a table lacks security columns (like TaskSeries), JOIN it with a related table that has it (like Projects).
                4. SCHEMA ADHERENCE: ONLY use tables and columns from the SCHEMA CONTEXT provided.
                5. PERMISSION: Never add invoice/payment/expense/lead/order tables unless those tables are present in SCHEMA CONTEXT.
                6. Return ONLY the corrected SQL string. No markdown, no explanation.
            ";

            var result = await CallGroqWithFallbackAsync(prompt, groqKey, model, GetFallbackModel(model), baseUrl);

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

        private string GetConfiguredModel()
            => _config["Groq:Model"] ?? DefaultModel;

        private string GetBaseUrl()
            => _config["Groq:BaseUrl"] ?? DefaultBaseUrl;

        private static string GetFallbackModel(string model)
            => model == SmartModel ? DefaultModel : SmartModel;

        private static string BuildHistoryContext(List<AIChatHistoryDto> history)
        {
            if (history == null || !history.Any())
            {
                return string.Empty;
            }

            var historyContext = new StringBuilder();
            historyContext.AppendLine("RECENT CONVERSATION HISTORY (Last 5 messages):");

            foreach (var message in history.TakeLast(5))
            {
                historyContext.AppendLine($"{message.Role.ToUpper()}: {message.Content}");
            }

            return historyContext.ToString();
        }

        private static HashSet<string> ResolveAllowedChatActions(IEnumerable<string> permissionClaims, bool isSuperAdmin)
        {
            var actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (isSuperAdmin)
            {
                actions.Add("create_lead");
                actions.Add("create_task");
                actions.Add("create_expense");
                return actions;
            }

            var claims = (permissionClaims ?? Enumerable.Empty<string>())
                .Select(NormalizePermissionText)
                .ToList();

            if (HasAnyAddPermission(claims, "lead", "leads", "lead_management", "leadmanagement"))
            {
                actions.Add("create_lead");
            }

            if (HasAnyAddPermission(claims, "task", "tasks", "task_management", "taskmanagement"))
            {
                actions.Add("create_task");
            }

            if (HasAnyAddPermission(claims, "expense", "expenses", "expense_management", "expensemanagement"))
            {
                actions.Add("create_expense");
            }

            return actions;
        }

        private static bool HasAnyAddPermission(IEnumerable<string> claims, params string[] moduleAliases)
            => claims.Any(claim =>
            {
                var parts = claim.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    return false;
                }

                return moduleAliases.Contains(parts[0], StringComparer.OrdinalIgnoreCase) &&
                       (parts[1].Equals("add", StringComparison.OrdinalIgnoreCase) ||
                        parts[1].Equals("create", StringComparison.OrdinalIgnoreCase));
            });

        private static string NormalizePermissionText(string value)
            => new string((value ?? string.Empty).Trim().ToLowerInvariant()
                    .Select(ch => char.IsLetterOrDigit(ch) || ch == ':' ? ch : '_')
                    .ToArray())
                .Replace("__", "_")
                .Trim('_');

        private async Task<(string Text, int Prompt, int Response, int Total)> CallGroqWithFallbackAsync(
            string prompt, string apiKey, string primaryModel, string fallbackModel, string baseUrl)
        {
            var result = await CallGroqAsync(prompt, apiKey, primaryModel, baseUrl);
            if (!string.IsNullOrEmpty(result.Text))
            {
                return result;
            }

            return await CallGroqAsync(prompt, apiKey, fallbackModel, baseUrl);
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
            if (string.IsNullOrEmpty(text)) return "{}";

            // Remove markdown code blocks if they exist
            if (text.Contains("```json"))
            {
                text = text.Replace("```json", "").Replace("```", "");
            }
            else if (text.Contains("```"))
            {
                text = text.Replace("```", "");
            }

            var start = text.IndexOf("{");
            var end   = text.LastIndexOf("}") + 1;
            if (start == -1 || end <= 0) return "{}";

            var json = text.Substring(start, end - start);
            
            // Clean up problematic characters but preserve valid structure
            // Instead of replacing ALL newlines, we only replace them if they aren't inside quotes (roughly)
            // But for simple cleaning, we'll just trim and ensure it's one line if possible for easier parsing.
            // Actually, JsonSerializer handles newlines fine, the issue is often extra text.
            return json.Trim();
        }

        private static string ExtractLastJsonObject(string text)
        {
            if (string.IsNullOrEmpty(text)) return "{}";

            var lastEnd = text.LastIndexOf("}");
            if (lastEnd == -1) return "{}";

            // Look backwards from the last '}' for the matching '{'
            int balance = 0;
            for (int i = lastEnd; i >= 0; i--)
            {
                if (text[i] == '}') balance++;
                if (text[i] == '{') balance--;

                if (balance == 0)
                {
                    return text.Substring(i, lastEnd - i + 1);
                }
            }

            return "{}";
        }
    }
}
