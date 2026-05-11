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
            var expenseCreateKeywords = new[] { "add expense", "create expense", "add expanse", "create expanse",
                "add exspense", "create exspense", "add expence", "create expence",
                "record expense", "save expense", "spent", "spending", "payment of", "bill for" };
            var expenseCategories = new[] { "travel", "food", "office", "utilities", "software", "miscellaneous" };
            var hasAmount = System.Text.RegularExpressions.Regex.IsMatch(msg, @"\d+");
            var hasCategory = expenseCategories.Any(c => msg.Contains(c));

            if (expenseCreateKeywords.Any(k => msg.Contains(k)) ||
                (hasAmount && hasCategory && (msg.Contains("expens") || msg.Contains("exspens") || msg.Contains("amount") || msg.Contains("paid") || msg.Contains("spend"))))
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

        public async Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isSuperAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null, string forceIntent = null)
        {
            var apiKey = _config["Groq:ApiKey"];
            var baseUrl = GetBaseUrl();

            var lowerMsg = userMessage.ToLower();

            // Context-aware intent detection — combine current message + last 3 USER messages only
            // (AI responses are excluded to prevent keywords in AI text from polluting intent detection)
            var detectionString = lowerMsg;
            if (history != null && history.Any())
            {
                var recentUserHistory = string.Join(" ", history.TakeLast(6)
                    .Where(h => h.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    .TakeLast(3)
                    .Select(h => h.Content.ToLower()));
                if (!string.IsNullOrEmpty(recentUserHistory))
                    detectionString = recentUserHistory + " " + lowerMsg;
            }

            var intents = string.IsNullOrEmpty(forceIntent)
                ? DetectIntents(detectionString)
                : new List<string> { forceIntent };

            var allowedChatActions = ResolveAllowedChatActions(allowedModules, isSuperAdmin);
            var allowedActionText = allowedChatActions.Count == 0 ? "none" : string.Join(", ", allowedChatActions);
            var historyContext = BuildHistoryContext(history);

            // Route to focused prompt: create or query
            var isCreate = intents.Any(i => i.StartsWith("create_"));

            string prompt;
            string model;

            if (isCreate)
            {
                // ── SMALL FOCUSED CREATE PROMPT ──────────────────────────────────────
                model = DefaultModel;
                var createIntent = intents.First(i => i.StartsWith("create_"));
                prompt = BuildCreatePrompt(userMessage, historyContext, allowedActionText, createIntent);
            }
            else
            {
                // ── FOCUSED QUERY PROMPT ─────────────────────────────────────────────
                var targetedSchema = AISchema.GetContextForIntent(intents, allowedModules, isSuperAdmin);
                var isComplex = intents.Count > 1 ||
                                lowerMsg.Contains("revenue") || lowerMsg.Contains("trend") ||
                                lowerMsg.Contains("compare") || lowerMsg.Contains("profit") ||
                                lowerMsg.Contains("report") || lowerMsg.Contains("summary") ||
                                lowerMsg.Contains("how many") || lowerMsg.Contains("top ");
                model = isComplex ? SmartModel : DefaultModel;
                prompt = BuildQueryPrompt(userMessage, targetedSchema, historyContext, allowedActionText);
            }

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

        // ── PROMPT BUILDERS ───────────────────────────────────────────────────────

        private static string BuildQueryPrompt(string userMessage, string targetedSchema, string historyContext, string allowedActionText)
        {
            return $@"You are a CRM T-SQL Expert for Avinya CRM (a printing and design business).
            Generate ONE SQL Server SELECT query for the user's question.

            MANDATORY SECURITY:
            - Every query MUST have: WHERE TenantId = @TenantId
            - Every query MUST have: IsDeleted = 0 (only on tables that have this column)
            - Tables without TenantId (LeadFollowups, OrderItems, QuotationItems, Payments, TaskSeries, TaskOccurrences, TeamMembers): JOIN parent table that has TenantId
            - ONLY SELECT allowed. Never UPDATE/DELETE/INSERT/DROP/EXEC
            - Use @TenantId and @UserId as parameters. Never hardcode GUIDs
            - SELECT TOP 50 by default. If user says ""all"", use TOP 200. Never generate a SELECT without TOP — max allowed is 200
            - Never select raw ID columns — always JOIN reference tables and show human-readable names

            TIME: Today is {DateTime.Now:yyyy-MM-dd}, Time: {DateTime.Now:HH:mm} (Year {DateTime.Now.Year})

            DATE PATTERNS (apply exactly):
            ""today""       → CAST(DateCol AS DATE) = CAST(GETDATE() AS DATE)
            ""this week""   → DATEPART(WEEK, DateCol) = DATEPART(WEEK, GETDATE()) AND YEAR(DateCol) = YEAR(GETDATE())
            ""this month""  → MONTH(DateCol) = MONTH(GETDATE()) AND YEAR(DateCol) = YEAR(GETDATE())
            ""last month""  → MONTH(DateCol) = MONTH(DATEADD(MONTH,-1,GETDATE())) AND YEAR(DateCol) = YEAR(DATEADD(MONTH,-1,GETDATE()))
            ""this year""   → YEAR(DateCol) = YEAR(GETDATE())
            ""last X days"" → DateCol >= DATEADD(DAY, -X, GETDATE())
            ""overdue""     → DateCol < GETDATE()
            ""upcoming""    → DateCol >= GETDATE()
            IMPORTANT: ""last 5"" or ""last 7"" WITHOUT the word 'days/weeks' = SELECT TOP N, NOT DATEADD

            REAL STATUS VALUES — use EXACTLY as written, no variations:
            Lead Status (StatusName):    'New' | 'Quotation Sent' | 'Converted' | 'JobWork In Process' | 'Dispatched To Customer' | 'Delivered/Done' | 'Lost'
            Lead Source (SourceName):    'Call' | 'Walk-in' | 'WhatsApp' | 'Referral' | 'Other Sources'
            Order Status (StatusID int): 1=Pending | 2=In Progress | 3=Inward Done | 4=Ready | 5=Delivered
            Design Status (int):         1=Pending | 2=In Progress | 3=Approved by Client | 4=Rejected
            Quotation Status (StatusName): 'Sent' | 'Accepted' | 'Rejected'
            Invoice Status (int):        1=Pending | 2=Partial | 3=Receive
            Followup Status (int):       1=Pending | 2=In Progress | 3=Completed
            Expense Category (CategoryName): 'Travel' | 'Food' | 'Office Supplies' | 'Utilities' | 'Software' | 'Miscellaneous' | 'Other'
            Project Status (StatusID int): 1=Planning | 2=Active | 3=Completed
            Project Priority (PriorityID int): 1=Low | 2=Medium | 3=High
            Task Status (varchar):       'Pending' | 'Completed' | 'Skipped'

            BUSINESS DEFINITIONS:
            Revenue     = SUM(Invoices.GrandTotal) WHERE IsDeleted=0
            Outstanding = SUM(Invoices.AmountAfterDiscount) WHERE IsDeleted=0  ← column is AmountAfterDiscount, NOT OutstandingAmount
            Collected   = SUM(Payments.Amount)
            Profit      = Revenue - SUM(Expenses.Amount) WHERE IsDeleted=0
            NEVER use column names: OutstandingAmount, TotalRevenue, TotalAmount — these do not exist in the DB

            QUERY RULES:
            1. GLOBAL SEARCH: If user provides a name/ID without specifying column, search across ALL human-readable columns using LIKE '%term%' with OR
            2. NEVER invent table names. Only use tables in the DATABASE CONTEXT below
            3. Notes/requirements for leads are in Leads.Notes and Leads.RequirementDetails
            4. For ""my leads"" / ""assigned to me"" → filter AssignedTo = @UserId or use the logged-in user name
            5. Always JOIN reference tables to show names not IDs (CompanyName, StatusName, SourceName, CategoryName etc.)

            PERMISSIONS: Use ONLY tables listed in DATABASE CONTEXT. If data not available, return action ""message"" explaining no permission.
            ALLOWED WRITE ACTIONS: {allowedActionText}

            DATABASE CONTEXT (JSON):
            {targetedSchema}

            CONVERSATION HISTORY:
            {historyContext}

            USER QUESTION: {userMessage}

            Return ONLY this JSON — no extra text, no markdown:
            {{
              ""action"": ""get_summary"",
              ""intent"": ""[query_leads|query_orders|query_invoices|query_expenses|query_clients|query_followups|query_tasks|query_projects|query_users|general_chat]"",
              ""sql"": ""YOUR_SINGLE_LINE_SQL_HERE"",
              ""parameters"": {{}},
              ""successMessage"": """",
              ""suggestions"": [""<3 short follow-up actions directly related to this query. RULES: Only suggest creating a Lead, Task, or Expense — these are the ONLY 3 things that can be created. Never suggest creating followups, orders, invoices, quotations, clients, or anything else. Prefer actionable next steps like 'Show converted leads', 'Add a new lead', 'Show pending tasks', 'Create a task', 'Show this month expenses', 'Add an expense'>""]
            }}";
        }

        private static string BuildCreatePrompt(string userMessage, string historyContext, string allowedActionText, string createIntent)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var now   = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            if (createIntent == "create_lead")
            {
                return $@"Extract lead creation parameters. Today: {today}. Allowed: {allowedActionText}

                RULES:
                - Required: ContactPerson, RequirementDetails. Default LeadStatusID: 'New'
                - Optional: CompanyName, Mobile, Email, ClientType (Individual|Company), LeadSourceID, NextFollowupDate, AssignedTo, Notes
                - Source: 'Call'|'Walk-in'|'WhatsApp'|'Referral'|'Other Sources'
                - Status: 'New'|'Quotation Sent'|'Converted'|'JobWork In Process'|'Dispatched To Customer'|'Delivered/Done'|'Lost'
                - Dates: 'tomorrow'={DateTime.Now.AddDays(1):yyyy-MM-dd} 'next week'={DateTime.Now.AddDays(7):yyyy-MM-dd} '30 june'={DateTime.Now.Year}-06-30
                - If user asks WHAT details are needed → action:""message"", errorMessage:""To create a lead I need: Contact person name and Requirement details. Optionally: company name, mobile, email, source, followup date, assigned to.""
                - NEVER invent Mobile/Email. If ContactPerson OR RequirementDetails missing → action:""message"", ask for it.
                - Check history for already-provided values and include them.
                {(string.IsNullOrEmpty(historyContext) ? "" : $"\nHISTORY:\n{historyContext}")}
                USER: {userMessage}

                Return ONLY JSON:
                {{""action"":""create_lead"",""intent"":""create_lead"",""sql"":"""",""parameters"":{{""ContactPerson"":"""",""RequirementDetails"":"""",""CompanyName"":"""",""Mobile"":"""",""Email"":"""",""ClientType"":"""",""LeadSourceID"":"""",""LeadStatusID"":""New"",""NextFollowupDate"":"""",""AssignedTo"":"""",""Notes"":""""}},""successMessage"":""Lead created for {{ContactPerson}}"",""errorMessage"":"""",""suggestions"":[""Show all my leads"",""Show today's followups"",""Add another lead""]}}";
            }

            if (createIntent == "create_task")
            {
                return $@"Extract task creation parameters. Today: {now}. Allowed: {allowedActionText}

                RULES:
                - Required: Title, DueDateTime (ISO), TaskType (Personal|Team)
                - Optional: Description, Notes, ListName, TeamName, AssignToName, ProjectName, ReminderAt
                - If user asks WHAT details are needed → action:""message"", errorMessage:""To create a task I need: Task title, Due date and time, Task type (Personal or Team). Optionally: team name, assign to someone, project name.""
                - If TaskType missing → action:""message"", ask ""Is this personal or for a team?""
                - If TaskType=Team and TeamName missing → action:""message"", ask which team
                - If DueDateTime missing → action:""message"", ask when
                - Check history for already-provided values.
                {(string.IsNullOrEmpty(historyContext) ? "" : $"\nHISTORY:\n{historyContext}")}
                USER: {userMessage}

                Return ONLY JSON:
                {{""action"":""create_task"",""intent"":""create_task"",""sql"":"""",""parameters"":{{""Title"":"""",""DueDateTime"":"""",""TaskType"":"""",""Description"":"""",""TeamName"":"""",""AssignToName"":"""",""ProjectName"":"""",""ReminderAt"":"""",""Notes"":""""}},""successMessage"":""Task '{{Title}}' scheduled"",""errorMessage"":"""",""suggestions"":[""Show my pending tasks"",""Show overdue tasks"",""Add another task""]}}";
            }

                // create_expense
                return $@"Extract expense creation parameters. Today: {today}. Allowed: {allowedActionText}

                RULES:
                - Required: Amount (number), CategoryName
                - Optional: ExpenseDate (default {today}), Description, PaymentMode, Status
                - Category: 'Travel'|'Food'|'Office Supplies'|'Utilities'|'Software'|'Miscellaneous'|'Other'
                - Status: 'Paid' if paid/spent, else 'Unpaid'
                - PaymentMode: Cash|UPI|Card|Bank Transfer|GPay|PhonePe
                - Extract Amount from any number in the message (e.g. ""2000"", ""₹500"", ""rs 1500"")
                - Extract CategoryName from context: 'travel'→'Travel', 'food'→'Food', 'office'→'Office Supplies', 'utility'/'utilities'→'Utilities', 'software'→'Software', else→'Miscellaneous'
                - If user asks WHAT details are needed → action:""message"", errorMessage:""To add an expense I need: Amount and Category (Travel, Food, Office Supplies, Utilities, Software, Miscellaneous, Other). Optionally: date, description, payment mode.""
                - ONLY ask for missing fields AFTER trying to extract them from the message. If Amount AND CategoryName found → proceed with create_expense action.
                - Check history for already-provided values.
                {(string.IsNullOrEmpty(historyContext) ? "" : $"\nHISTORY:\n{historyContext}")}
                USER: {userMessage}

                Return ONLY JSON:
                {{""action"":""create_expense"",""intent"":""create_expense"",""sql"":"""",""parameters"":{{""Amount"":"""",""CategoryName"":"""",""ExpenseDate"":""{today}"",""Description"":"""",""PaymentMode"":"""",""Status"":""Unpaid""}},""successMessage"":""Expense of ₹{{Amount}} recorded"",""errorMessage"":"""",""suggestions"":[""Show this month's expenses"",""Show total expenses by category"",""Add another expense""]}}";
        }

        // ── HUMAN RESPONSE FORMATTER ─────────────────────────────────────────────

        public async Task<string> FormatHumanResponseAsync(string userMessage, List<Dictionary<string, object>> data, int count)
        {
            var apiKey = _config["Groq:FormatApiKey"] ?? _config["Groq:ApiKey"];
            var baseUrl = GetBaseUrl();

            var dataSummary = BuildDataSummary(data);

            var prompt = $@"You are Avinya, a friendly assistant for a printing and design business CRM.

                User asked: ""{userMessage}""
                Records found: {count}
                Sample data: {dataSummary}

                Write a natural, warm 1-2 sentence response. Rules:
                - Use actual names and numbers from the data (CompanyName, StatusName, amounts etc.)
                - NEVER say ""I found X results"" or ""Here are the results"" or ""Based on the data""
                - If 0 records: explain what was checked and suggest what the user could try instead
                - If data exists: mention specific highlights (e.g. top name, total amount, who has most)
                - End with ONE short follow-up question only if it adds value
                - Plain conversational text only — no bullet points, no markdown, no lists

                Response:";

            var result = await CallGroqAsync(prompt, apiKey, DefaultModel, baseUrl);

            if (string.IsNullOrWhiteSpace(result.Text))
                return count == 0
                    ? "No records found matching your request."
                    : $"Found {count} records for your query.";

            return result.Text.Trim();
        }

        private static string BuildDataSummary(List<Dictionary<string, object>> data)
        {
            if (data == null || !data.Any()) return "No records found.";

            // If it's a JSON result (FOR JSON PATH query), just indicate count
            if (data.Count == 1 && data[0].ContainsKey("JsonResult"))
                return "Summary data returned (aggregated report).";

            // Take first 5 rows, first 6 columns of each
            var rows = data.Take(5).Select(row =>
            {
                var fields = row
                    .Where(kv => kv.Value != null && !kv.Key.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
                    .Take(6)
                    .Select(kv =>
                    {
                        var val = kv.Value?.ToString() ?? "";
                        // Format dates nicely
                        if (DateTime.TryParse(val, out var dt))
                            val = dt.ToString("dd MMM yyyy");
                        return $"{kv.Key}: {val}";
                    });
                return string.Join(", ", fields);
            });

            return string.Join(" | ", rows);
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

   

        public async Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, string userId, bool isSuperAdmin, List<AIChatHistoryDto>? history = null, List<string>? allowedModules = null)
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
