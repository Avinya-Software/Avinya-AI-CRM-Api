using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.DTOs.Expense;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.Clients;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.AI;
using Dapper;
using Microsoft.EntityFrameworkCore;
using AvinyaAICRM.Application.AI.Pipeline;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Expense;
using AvinyaAICRM.Domain.Constant;
using System.Text.RegularExpressions;
using AvinyaAICRM.Domain.Enums.Clients;

namespace AvinyaAICRM.Application.Services.AICHATS
{
    public class CRMQueryService : ICRMQueryService
    {
        private readonly AppDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILeadService _leadService;
        private readonly ITaskService _taskService;
        private readonly AIPipeline _pipeline;
        private readonly ICreditService _credits;
        private readonly IAIKnowledgeService _knowledge;
        private readonly IClientRepository _clientRepo;
        private readonly IExpenseService _expenseService;

        public CRMQueryService(
            AppDbContext context,
            IAIService aiService,
            ILeadService leadService,
            ITaskService taskService,
            AIPipeline pipeline,
            ICreditService credits,
            IAIKnowledgeService knowledge,
            IClientRepository clientRepo,
            IExpenseService expenseService)
        {
            _context = context;
            _aiService = aiService;
            _leadService = leadService;
            _taskService = taskService;
            _pipeline = pipeline;
            _credits = credits;
            _knowledge = knowledge;
            _clientRepo = clientRepo;
            _expenseService = expenseService;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, bool isSuperAdmin, string userId = "", string contextMessage = "", List<string> allowedModules = null)
        {
            allowedModules ??= await GetUserAiPermissionClaimsAsync(userId, isSuperAdmin);
            return await ExecuteRawSqlWithHealingAsync(sql, tenantId, isSuperAdmin, contextMessage, userId, null, allowedModules);
        }


        /// Injects SELECT TOP 200 if the SQL has no TOP clause — hard cap to prevent runaway queries.
        private static string InjectTopIfMissing(string sql, int maxRows = 200)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;
            // Skip FOR JSON PATH queries — they aggregate, no row cap needed
            if (sql.Contains("FOR JSON", StringComparison.OrdinalIgnoreCase)) return sql;
            // Already has TOP — leave it alone
            if (Regex.IsMatch(sql, @"\bSELECT\s+TOP\s*\(?\d+\)?", RegexOptions.IgnoreCase)) return sql;
            // Inject TOP after SELECT
            return Regex.Replace(sql, @"\bSELECT\b", $"SELECT TOP {maxRows}", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }

        private async Task<List<Dictionary<string, object>>> ExecuteRawSqlWithHealingAsync(
            string sql, Guid tenantId, bool isSuperAdmin, string originalMessage, string userId = "",
            List<AIChatHistoryDto> history = null, List<string> allowedModules = null)
        {
            if (string.IsNullOrWhiteSpace(sql)) return new List<Dictionary<string, object>>();
            sql = InjectTopIfMissing(sql);
            allowedModules ??= await GetUserAiPermissionClaimsAsync(userId, isSuperAdmin);

            // ── PHASE 1: Validate + Execute the original SQL ─────────────────────────
            string healReason = null;

            try
            {
                // Safety guard: block mutating statements
                var forbidden = new[] { "UPDATE", "DELETE", "DROP", "INSERT", "ALTER", "TRUNCATE", "CREATE" };
                if (!isSuperAdmin && sql.ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => forbidden.Contains(w)))
                    throw new UnauthorizedAccessException("Only SELECT queries are allowed for safety.");

                ValidateSqlTableAccess(sql, allowedModules, isSuperAdmin);

                // Security guard: must contain TenantId filter
                if (!isSuperAdmin &&
                    !sql.Contains("@TenantId", StringComparison.OrdinalIgnoreCase) &&
                    !sql.Contains(tenantId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    healReason = "Security Validation Failed: The generated SQL does not filter by TenantId. " +
                                 "You MUST add a WHERE or JOIN condition with TenantId = @TenantId.";
                }
                else
                {
                    return await RunSqlAsync(sql, tenantId, userId);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                healReason = $"SQL Execution Error: {ex.Message}";
            }

            // ── PHASE 2: One-time AI Heal ────────────────────────────────────────────
            if (string.IsNullOrEmpty(originalMessage))
                throw new Exception(healReason ?? "Query failed and no context was available to heal it.");

            var healedSql = await _aiService.FixSqlAsync(
                sql,
                healReason,
                originalMessage,
                tenantId,
                userId,
                isSuperAdmin,
                history,
                allowedModules);

            if (string.IsNullOrWhiteSpace(healedSql))
                throw new Exception($"SQL healing returned no result. Original error: {healReason}");

            // ── PHASE 3: Execute healed SQL exactly once ─────────────────────────────
            try
            {
                ValidateSqlTableAccess(healedSql, allowedModules, isSuperAdmin);
                return await RunSqlAsync(healedSql, tenantId, userId);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Healed SQL also failed. Heal reason: {healReason} | Healed SQL error: {ex.Message}");
            }
        }

        /// <summary>Executes a SQL string and returns results as a list of dictionaries.</summary>
        private async Task<List<Dictionary<string, object>>> RunSqlAsync(string sql, Guid tenantId, string userId)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString());
            var queryParams = new { TenantId = tenantId, UserId = userId };
            var results = (await connection.QueryAsync(sql, queryParams)).ToList();

            if (results.Any())
            {
                var firstRow = (IDictionary<string, object>)results[0];
                var jsonKey = firstRow.Keys.FirstOrDefault(k => k.StartsWith("JSON_"));

                if (jsonKey != null)
                {
                    // Handle SQL Server FOR JSON multi-line fragmentation
                    var fullJson = string.Concat(results.Select(r => ((IDictionary<string, object>)r)[jsonKey]?.ToString() ?? ""));
                    return new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "JsonResult", fullJson } }
                    };
                }
            }

            return results
                .Select(x => (Dictionary<string, object>)new Dictionary<string, object>((IDictionary<string, object>)x))
                .ToList();
        }


        public async Task<AIResponse> ProcessChatRequestAsync(AIRequest request, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules)
        {
            // 0. Ensure credits record exists for the user
            await _credits.EnsureUserCreditExistsAsync(userId, tenantId);
            var permissionClaims = await GetUserAiPermissionClaimsAsync(userId, isSuperAdmin);
            foreach (var module in allowedModules ?? new List<string>())
            {
                permissionClaims.Add(module);
            }

            // 1. Process Command (Intent + SQL Generation)
            var response = await ProcessCommandAsync(request.Message, tenantId, userId, isSuperAdmin, permissionClaims, request.History);

            // 1.5. Deduct Credits immediately for the initial AI call
            if (response.TotalTokens > 0)
            {
                response.CreditsUsed = await _credits.DeductCreditsForTokenUsageAsync(
                    userId,
                    response.TotalTokens,
                    response.Source.ToUpper());
                response.RemainingCredits = await _credits.GetRemainingCreditsAsync(userId);
            }

            // 2. If a receipt file is uploaded but intent wasn't expense, re-route to expense creation
            if (request.ReceiptFile != null && response.Action != "create_expense")
            {
                // File uploads are always expense receipts — override and reprocess as expense
                var expenseResponse = await _aiService.AnalyzeMessageAsync(
                    request.Message, tenantId, isSuperAdmin, permissionClaims, request.History, forceIntent: "create_expense");
                expenseResponse.CreditsUsed = response.CreditsUsed;
                expenseResponse.RemainingCredits = response.RemainingCredits;
                response = expenseResponse;
            }

            var writeActions = new[] { "create_lead", "create_task", "create_expense" };

            // 3. Handle Execution
            // If it's a query (Sql exists) AND it's NOT a creation action, execute it.
            if (!string.IsNullOrEmpty(response.Sql) && !writeActions.Contains(response.Action))
            {
                try 
                {
                    var data = await ExecuteRawSqlWithHealingAsync(response.Sql, tenantId, isSuperAdmin, request.Message, userId, request.History, permissionClaims);

                    response.Data = data;
                    response.Count = data.Count;
                    response.Query = response.Sql;


                    // 3b. Automatic Knowledge Recording (First time queries)
                    if (response.Source == "ai_sql")
                    {
                        await _knowledge.RecordFirstTimeQueryAsync(request.Message, response.Sql, userId);
                    }

                    // 4. AI writes a human-like response using actual data values
                    response.Message = await _aiService.FormatHumanResponseAsync(request.Message, data, data.Count);
                    response.SuccessMessage = null; // prevent static successMessage from overriding the AI-formatted message
                    ConsolidateMessage(response);
                }
                catch (UnauthorizedAccessException ex)
                {
                    response.ErrorMessage = ex.Message;
                    response.Message = ex.Message;
                }
                catch (Exception ex)
                {
                    // Log internally but never expose raw SQL errors to the user
                    _ = ex.Message; // available for logging if needed
                    response.ErrorMessage = null;
                    response.Message = "Sorry, I wasn't able to retrieve that data right now. Please try rephrasing your question or ask something else.";
                }

                ConsolidateMessage(response);
                return response; // Exit after query handling
            }

            // 4. Handle Specific Actions
            if (writeActions.Contains(response.Action) && !CanRunWriteAction(response.Action, permissionClaims, isSuperAdmin))
            {
                SetEmptyResponse(response, $"You don't have permission to {GetFriendlyActionName(response.Action)}.");
                response.Action = "message";
                return response;
            }

            if (response.Action == "create_lead")
            {
                try
                {
                    // 1. Validate required parameters (AI should have filled these)
                    var companyName = response.Parameters?.ContainsKey("CompanyName") == true ? response.Parameters["CompanyName"]?.ToString() : null;
                    var contactPerson = response.Parameters?.ContainsKey("ContactPerson") == true ? response.Parameters["ContactPerson"]?.ToString() : null;
                    var requirements = response.Parameters?.ContainsKey("RequirementDetails") == true ? response.Parameters["RequirementDetails"]?.ToString() : null;
                    var clientType = response.Parameters?.ContainsKey("ClientType") == true ? response.Parameters["ClientType"]?.ToString() : null;

                    if (string.IsNullOrEmpty(requirements))
                    {
                        response.Action = "message";
                        response.Message = "I need the requirement details to create a lead.";
                        return response;
                    }

                    if (string.IsNullOrEmpty(contactPerson))
                    {
                        response.Action = "message";
                        response.Message = "Please provide the name of the contact person for this lead.";
                        return response;
                    }

                    // 2. Extract and fill LeadRequestDto
                    var dto = new LeadRequestDto
                    {
                        CompanyName = string.IsNullOrEmpty(companyName) ? contactPerson : companyName,
                        RequirementDetails = requirements,
                        ContactPerson = contactPerson,
                        Mobile = response.Parameters.ContainsKey("Mobile") ? response.Parameters["Mobile"]?.ToString() : null,
                        Email = response.Parameters.ContainsKey("Email") ? response.Parameters["Email"]?.ToString() : null,
                        GSTNo = response.Parameters.ContainsKey("GSTNo") ? response.Parameters["GSTNo"]?.ToString() : null,
                        BillingAddress = response.Parameters.ContainsKey("BillingAddress") ? response.Parameters["BillingAddress"]?.ToString() : null,
                        OtherSources = response.Parameters.ContainsKey("OtherSources") ? response.Parameters["OtherSources"]?.ToString() : null,
                        Notes = response.Parameters.ContainsKey("Notes") ? response.Parameters["Notes"]?.ToString() : null,
                        Links = response.Parameters.ContainsKey("Links") ? response.Parameters["Links"]?.ToString() : null,
                    };

                    dto.ClientType = (int)ClientTypeEnum.Company; // Default to 1 (Company)

                    if (response.Parameters.TryGetValue("ClientType", out var cType) && cType != null)
                    {
                        var typeStr = cType.ToString();
                        if (typeStr.Equals("Individual", StringComparison.OrdinalIgnoreCase) || typeStr == "2")
                        {
                            dto.ClientType = (int)ClientTypeEnum.Individual;
                        }
                        else if (typeStr.Equals("Company", StringComparison.OrdinalIgnoreCase) || typeStr == "1")     
                        {
                            dto.ClientType = (int)ClientTypeEnum.Company;
                        }
                    }

                    // --- RESOLVE NAMES TO IDs ---
                    
                    // 1. Resolve State & City
                    if (response.Parameters.TryGetValue("StateID", out var stateNameObj) && !string.IsNullOrEmpty(stateNameObj?.ToString()))
                    {
                        var stateName = stateNameObj.ToString();
                        var state = await _context.States.FirstOrDefaultAsync(s => s.StateName.Contains(stateName));
                        if (state != null) dto.StateID = state.StateID;
                    }
                    if (response.Parameters.TryGetValue("CityID", out var cityNameObj) && !string.IsNullOrEmpty(cityNameObj?.ToString()))
                    {
                        var cityName = cityNameObj.ToString();
                        var city = await _context.Cities.FirstOrDefaultAsync(c => c.CityName.Contains(cityName));
                        if (city != null) 
                        {
                            dto.CityID = city.CityID;
                            if (dto.StateID == null) dto.StateID = city.StateID; // Auto-set state if city found
                        }
                    }

                    // 2. Resolve Lead Source & Status
                    if (response.Parameters.TryGetValue("LeadSourceID", out var sourceNameObj) && !string.IsNullOrEmpty(sourceNameObj?.ToString()))
                    {
                        var sourceName = sourceNameObj.ToString();
                        var source = await _context.leadSourceMasters.FirstOrDefaultAsync(s => s.SourceName.Contains(sourceName) && s.IsActive);
                        if (source != null) dto.LeadSourceID = source.LeadSourceID;
                    }
                    if (response.Parameters.TryGetValue("LeadStatusID", out var statusNameObj) && !string.IsNullOrEmpty(statusNameObj?.ToString()))
                    {
                        var statusName = statusNameObj.ToString();
                        var status = await _context.leadStatusMasters.FirstOrDefaultAsync(s => s.StatusName.Contains(statusName) && s.IsActive);
                        if (status != null) dto.LeadStatusID = status.LeadStatusID;
                    }

                    // Fallback: If no status specified, default to "New"
                    if (dto.LeadStatusID == null)
                    {
                        var defaultStatus = await _context.leadStatusMasters.FirstOrDefaultAsync(s => s.StatusName.ToLower() == "new" && s.IsActive);
                        if (defaultStatus != null) dto.LeadStatusID = defaultStatus.LeadStatusID;
                    }

                    // 3. Resolve AssignedTo (User)
                    if (response.Parameters.TryGetValue("AssignedTo", out var userNameObj) && !string.IsNullOrEmpty(userNameObj?.ToString()))
                    {
                        var userName = userNameObj.ToString();
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName.Contains(userName) && u.TenantId == tenantId);
                        if (user != null) dto.AssignedTo = user.Id;
                        else dto.AssignedTo = userName; // Fallback to raw value if not found
                    }

                    // 4. Handle Dates
                    if (response.Parameters.TryGetValue("NextFollowupDate", out var fDateObj) && DateTime.TryParse(fDateObj?.ToString(), out var followDate)) 
                        dto.NextFollowupDate = followDate;



                    // 3. Search for existing client (Prioritize ContactPerson)
                    var existingClients = await _clientRepo.FindByContactPersonAsync(contactPerson, tenantId);
                    
                    if (existingClients.Any())
                    {
                        bool hasDisambiguationInfo = !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Mobile) || !string.IsNullOrEmpty(companyName);
                        
                        var matchedClients = existingClients.ToList();
                        if (!string.IsNullOrEmpty(dto.Email))
                            matchedClients = matchedClients.Where(c => c.Email == dto.Email).ToList();
                        if (!string.IsNullOrEmpty(dto.Mobile))
                            matchedClients = matchedClients.Where(c => c.Mobile == dto.Mobile).ToList();
                        if (!string.IsNullOrEmpty(companyName))
                            matchedClients = matchedClients.Where(c => c.CompanyName.ToLower().Contains(companyName.ToLower())).ToList();

                        if (matchedClients.Count != 1 || !hasDisambiguationInfo)
                        {
                            response.Action = "message";
                            response.Message = $"A contact person named '{contactPerson}' already exists. Can you please share his mobile number or email to know which contact person?";
                            return response;
                        }

                        existingClients = matchedClients;
                    }

                    var client = existingClients.FirstOrDefault();
                    if (client != null)
                    {
                        dto.ClientID = client.ClientID;
                        // Use existing client info if DTO is missing it
                        dto.ContactPerson ??= client.ContactPerson;
                        dto.Mobile ??= client.Mobile;
                        dto.Email ??= client.Email;
                        dto.BillingAddress ??= client.BillingAddress;
                        dto.StateID ??= client.StateID;
                        dto.CityID ??= client.CityID;
                        dto.CompanyName = client.CompanyName;
                    }
                    else if (!string.IsNullOrEmpty(companyName))
                    {
                        // If no contact person match, try searching by company name as a fallback
                        var clientsByCompany = await _clientRepo.FindByNameAsync(companyName, tenantId);
                        var companyClient = clientsByCompany.FirstOrDefault();
                        if (companyClient != null)
                        {
                            dto.ClientID = companyClient.ClientID;
                            dto.ContactPerson ??= companyClient.ContactPerson;
                            dto.Mobile ??= companyClient.Mobile;
                            dto.Email ??= companyClient.Email;
                            dto.BillingAddress ??= companyClient.BillingAddress;
                            dto.StateID ??= companyClient.StateID;
                            dto.CityID ??= companyClient.CityID;
                            dto.CompanyName = companyClient.CompanyName;
                        }
                    }

                    // 4. Create Lead
                    var result = await _leadService.CreateAsync(dto, userId);
                    if (result.StatusCode == 200)
                    {
                        var template = !string.IsNullOrWhiteSpace(response.SuccessMessage) 
                            ? response.SuccessMessage 
                            : $"I've successfully created the lead for {dto.CompanyName}.";
                            
                        response.Message = FormatMessage(template, response);

                        // We no longer return the created lead data to avoid redundant UI rendering
                        // The frontend uses parameters to show the success card
                        if (result.Data != null)
                        {
                            response.Data = null;
                            response.Count = 0;
                        }

                    }
                    else
                    {
                        response.ErrorMessage = result.StatusMessage;
                        response.Message = "I tried to create the lead but failed: " + result.StatusMessage;
                        response.Suggestions = new List<string> { "Can I help you with any other leads?" };
                    }
                }
                catch (Exception ex)
                {
                    response.ErrorMessage = "Lead creation failed: " + ex.Message;
                    response.Message = response.ErrorMessage;
                }
            }
            else if (response.Action == "create_task")
            {
                try
                {
                    // 1. Validate required parameters
                    var title = response.Parameters?.ContainsKey("Title") == true ? response.Parameters["Title"]?.ToString() : null;
                    var dueDateStr = response.Parameters?.ContainsKey("DueDateTime") == true ? response.Parameters["DueDateTime"]?.ToString() : null;
                    var taskType = response.Parameters?.ContainsKey("TaskType") == true ? response.Parameters["TaskType"]?.ToString() : null;

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(dueDateStr) || string.IsNullOrEmpty(taskType))
                    {
                        response.Message = response.ErrorMessage ?? "I need a title, due date, and task type (Personal/Team) to create a task.";
                        return response;
                    }

                    // 2. Create DTO
                    var dto = new CreateTaskDto
                    {
                        Title = title,
                        Description = response.Parameters.ContainsKey("Description") ? response.Parameters["Description"]?.ToString() : null,
                        Notes = response.Parameters.ContainsKey("Notes") ? response.Parameters["Notes"]?.ToString() : null,
                        Scope = taskType,
                        Status = "Pending"
                    };

                    // 3. Resolve Due Date
                    if (DateTime.TryParse(dueDateStr, out var dueDate))
                    {
                        if (dueDate.Kind == DateTimeKind.Unspecified || dueDate.Kind == DateTimeKind.Local)
                        {
                            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            dto.DueDateTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dueDate, DateTimeKind.Unspecified), istZone);
                        }
                        else
                        {
                            dto.DueDateTime = dueDate;
                        }
                    }

                    // 4. Resolve List
                    if (response.Parameters.TryGetValue("ListName", out var listNameObj) && !string.IsNullOrEmpty(listNameObj?.ToString()))
                    {
                        var listName = listNameObj.ToString();
                        var list = await _context.TaskLists.FirstOrDefaultAsync(l => l.Name.Contains(listName) && (l.OwnerId == Guid.Parse(userId) || l.TeamId != null));
                        if (list != null) dto.ListId = list.Id;
                    }

                    // 5. Resolve Team
                    if (taskType.Equals("Team", StringComparison.OrdinalIgnoreCase))
                    {
                        var teamName = response.Parameters.ContainsKey("TeamName") ? response.Parameters["TeamName"]?.ToString() : null;
                        if (!string.IsNullOrEmpty(teamName))
                        {
                            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name.Contains(teamName) && t.TenantId == tenantId);
                            if (team != null) dto.TeamId = team.Id;
                        }
                    }

                    // 6. Resolve Assigned User
                    if (response.Parameters.TryGetValue("AssignToName", out var assignNameObj) && !string.IsNullOrEmpty(assignNameObj?.ToString()))
                    {
                        var assignName = assignNameObj.ToString();
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName.Contains(assignName) && u.TenantId == tenantId);
                        if (user != null) dto.AssignToId = user.Id;
                    }

                    // 7. Resolve Project
                    if (response.Parameters.TryGetValue("ProjectName", out var projNameObj) && !string.IsNullOrEmpty(projNameObj?.ToString()))
                    {
                        var projName = projNameObj.ToString();
                        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName.Contains(projName) && p.TenantId == tenantId);
                        if (project != null) dto.ProjectId = project.ProjectID.ToString();
                    }

                    // 8. Handle Reminders and Recurrence
                    if (response.Parameters.TryGetValue("ReminderAt", out var remObj) && DateTime.TryParse(remObj?.ToString(), out var reminderDate)) 
                        dto.ReminderAt = reminderDate;
                    
                    if (response.Parameters.TryGetValue("IsRecurring", out var recObj) && bool.TryParse(recObj?.ToString(), out var isRecur)) 
                        dto.IsRecurring = isRecur;
                    
                    if (response.Parameters.TryGetValue("RecurrenceRule", out var rRuleObj)) 
                        dto.RecurrenceRule = rRuleObj?.ToString();

                    // 9. Create Task
                    var result = await _taskService.CreateTaskAsync(dto, userId);
                    if (result.StatusCode == 200)
                    {
                        var template = !string.IsNullOrWhiteSpace(response.SuccessMessage) 
                            ? response.SuccessMessage 
                            : $"I've successfully created the task: {title}.";
                            
                        response.Message = FormatMessage(template, response);

                        // We no longer return the created task data to avoid redundant UI rendering
                        if (result.Data != null)
                        {
                            response.Data = null;
                            response.Count = 0;
                        }

                    }
                    else
                    {
                        response.ErrorMessage = result.StatusMessage;
                        response.Message = "I tried to create the task but failed: " + result.StatusMessage;
                        response.Suggestions = new List<string> { "Can I help you with any other tasks?" };
                    }
                }
                catch (Exception ex)
                {
                    response.ErrorMessage = "Task creation failed: " + ex.Message;
                    response.Message = response.ErrorMessage;
                }
            }
            else if (response.Action == "create_expense")
            {
                try
                {
                    // 1. Validate required parameters
                    var amountStr = response.Parameters?.ContainsKey("Amount") == true ? response.Parameters["Amount"]?.ToString() : null;
                    var categoryName = response.Parameters?.ContainsKey("CategoryName") == true ? response.Parameters["CategoryName"]?.ToString() : null;

                    if (string.IsNullOrEmpty(amountStr) || string.IsNullOrEmpty(categoryName))
                    {
                        response.Message = response.ErrorMessage ?? "I need an amount and a category (e.g., Travel, Office) to record an expense.";
                        return response;
                    }

                    if (!decimal.TryParse(amountStr, out var amount))
                    {
                        response.Message = "The amount provided is not a valid number.";
                        return response;
                    }

                    // 2. Resolve Category
                    var category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.CategoryName.Contains(categoryName));
                    if (category == null)
                    {
                        category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.CategoryName.Contains("Other"))
                                ?? await _context.ExpenseCategories.FirstOrDefaultAsync();
                    }
                    var categoryId = category?.CategoryId ?? 1; 

                    // 3. Create DTO
                    var dto = new CreateExpenseDto
                    {
                        Amount = amount,
                        CategoryId = categoryId,
                        ExpenseDate = DateTime.Now,
                        Description = response.Parameters.ContainsKey("Description") ? response.Parameters["Description"]?.ToString() : response.Parameters.ContainsKey("Notes") ? response.Parameters["Notes"]?.ToString() : "Created via AI Chat",
                        PaymentMode = !string.IsNullOrEmpty(response.Parameters.ContainsKey("PaymentMode") ? response.Parameters["PaymentMode"]?.ToString() : null) 
                                      ? response.Parameters["PaymentMode"].ToString() 
                                      : "Cash",
                        Status = response.Parameters.ContainsKey("Status") 
                                  ? response.Parameters["Status"].ToString() 
                                  : "Unpaid",
                        ReceiptFile = request.ReceiptFile // Attach the file from the chat request
                    };

                    if (response.Parameters.TryGetValue("ExpenseDate", out var dateObj) && dateObj != null && DateTime.TryParse(dateObj.ToString(), out var expDate))
                        dto.ExpenseDate = expDate;

                    // 4. Create Expense
                    var result = await _expenseService.CreateAsync(dto, tenantId.ToString(), Guid.Parse(userId));
                    if (result.StatusCode == 200)
                    {
                        var template = !string.IsNullOrWhiteSpace(response.SuccessMessage)
                            ? response.SuccessMessage
                            : $"I've recorded the expense of ₹{amount} for {categoryName}.";

                        response.Message = FormatMessage(template, response);
                        response.Data = null;
                        response.Count = 0;

                    }
                    else
                    {
                        response.ErrorMessage = result.StatusMessage;
                        response.Message = "I tried to record the expense but failed: " + result.StatusMessage;
                        response.Suggestions = new List<string> { "Can I help you with any other expenses?" };
                    }
                }
                catch (Exception ex)
                {
                    response.ErrorMessage = "Expense creation failed: " + ex.Message;
                    response.Message = response.ErrorMessage;
                }
            }
            else
            {
                ConsolidateMessage(response);
            }

            return response;
        }

        private static void ConsolidateMessage(AIResponse response)
        {
            // If message is already set by high-level logic, keep it unless it's empty
            var finalMessage = !string.IsNullOrWhiteSpace(response.ErrorMessage) ? response.ErrorMessage
                             : !string.IsNullOrWhiteSpace(response.ClarificationMessage) ? response.ClarificationMessage
                             : !string.IsNullOrWhiteSpace(response.SuccessMessage) ? response.SuccessMessage
                             : !string.IsNullOrWhiteSpace(response.Message) ? response.Message
                             : "I'm here to help! What can I do for you?";

            response.Message = finalMessage;
        }

        private static void SetEmptyResponse(AIResponse response, string message)
        {
            response.Message = message;
            response.Data = new List<Dictionary<string, object>>();
            response.Count = 0;
        }

        private string FormatMessage(string template, AIResponse response)
        {
            if (string.IsNullOrEmpty(template)) return "";

            var formatted = template;

            // 1. Replace placeholders from Data (if any)
            if (response.Data != null && response.Data.Any())
            {
                var row = response.Data[0];
                
                // Handle standard {Key} placeholders
                foreach (var kvp in row)
                {
                    string val = kvp.Value?.ToString() ?? "";
                    if (kvp.Value is DateTime dt) val = dt.ToString("dd MMM yyyy HH:mm");
                    else if (kvp.Key.Contains("Date") || kvp.Key.Contains("Time"))
                    {
                        if (DateTime.TryParse(val, out var dtParsed)) val = dtParsed.ToString("dd MMM yyyy HH:mm");
                    }
                    if (string.IsNullOrEmpty(val) || val == "0") val = "N/A";
                    
                    formatted = ReplaceTemplateToken(formatted, kvp.Key, val);
                }

                // Handle JsonResult
                if (row.ContainsKey("JsonResult"))
                {
                    var jsonStr = row["JsonResult"]?.ToString();
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        try 
                        {
                            var dashboard = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                            if (dashboard != null)
                            {
                                foreach (var kvp in dashboard)
                                    formatted = ReplaceTemplateToken(formatted, kvp.Key, kvp.Value?.ToString() ?? "0");
                            }
                        } catch { }
                    }
                }
            }

            // 2. Replace placeholders from Parameters
            if (response.Parameters != null)
            {
                foreach (var kvp in response.Parameters)
                {
                    formatted = ReplaceTemplateToken(formatted, kvp.Key, kvp.Value?.ToString() ?? "");
                }
            }

            // 3. Handle {count} - Special Case
            // If it's an action, {count} usually doesn't make sense unless it's a search.
            // But we'll replace it with response.Count or 0 if not set.
            formatted = formatted.Replace("{count}", response.Count.ToString())
                                 .Replace("[count]", response.Count.ToString())
                                 .Replace("{{count}}", response.Count.ToString());

            // 4. Final Cleanup: If any {Placeholder} remains, it's likely a hallucination or empty data
            // We'll replace them with "..." or empty string to avoid showing raw braces to user
            // However, we'll keep it for now to help debug if needed, or replace with N/A
            
            return formatted;
        }

        private static string ReplaceTemplateToken(string template, string key, string value)
            => template.Replace("{" + key + "}", value)
                       .Replace("[" + key + "]", value)
                       .Replace("{{" + key + "}}", value);

        public async Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null)
        {
            // Use the new Pipeline
            var pipelineResult = await _pipeline.ProcessAsync(message, tenantId, userId, isSuperAdmin, allowedModules, history);

            var aiResponse = new AIResponse
            {
                Action = pipelineResult.Action ?? "message",
                Intent = pipelineResult.Intent,
                Sql = pipelineResult.Sql,
                Parameters = pipelineResult.Parameters,
                ClarificationMessage = pipelineResult.ClarificationMessage,
                IsClarificationRequired = pipelineResult.IsClarificationRequired,
                SuccessMessage = pipelineResult.SuccessMessage,
                ErrorMessage = pipelineResult.ErrorMessage,
                PromptTokens = pipelineResult.PromptTokens,
                ResponseTokens = pipelineResult.ResponseTokens,
                TotalTokens = pipelineResult.TotalTokens,
                CreditsUsed = pipelineResult.CreditsUsed,
                RemainingCredits = pipelineResult.RemainingCredits,
                Source = pipelineResult.Source,
                Suggestions = pipelineResult.Suggestions
            };

            return aiResponse;
        }

        private void ValidateSqlTableAccess(string sql, List<string> permissionClaims, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                return;
            }

            var usedTables = ExtractSqlTables(sql);
            if (!usedTables.Any())
            {
                return;
            }

            var allowedTables = AISchema.ResolveAllowedTableNames(permissionClaims, isSuperAdmin);
            var deniedTables = usedTables
                .Where(t => !allowedTables.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (deniedTables.Any())
            {
                var deniedAreas = deniedTables
                    .Select(GetFriendlyAccessArea)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                throw new UnauthorizedAccessException(
                    $"You don't have permission to access {string.Join(", ", deniedAreas)}.");
            }
        }

        private static HashSet<string> ExtractSqlTables(string sql)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(sql))
            {
                return tables;
            }

            foreach (Match match in Regex.Matches(
                sql,
                @"\b(?:FROM|JOIN|APPLY)\s+(?:\[[^\]]+\]\.)?(?:dbo\.)?\[?(?<table>[A-Za-z_][A-Za-z0-9_]*)\]?",
                RegexOptions.IgnoreCase))
            {
                var tableName = match.Groups["table"].Value;
                if (AISchema.TableNames.Contains(tableName))
                {
                    tables.Add(tableName);
                }
            }

            return tables;
        }

        private async Task<List<string>> GetUserAiPermissionClaimsAsync(string userId, bool isSuperAdmin)
        {
            if (string.IsNullOrWhiteSpace(userId) && !isSuperAdmin)
            {
                return new List<string>();
            }

            var query =
                from p in _context.Permissions
                join m in _context.Modules on p.ModuleId equals m.ModuleId
                join a in _context.Actions on p.ActionId equals a.ActionId
                where m.IsActive
                select new
                {
                    m.ModuleKey,
                    m.ModuleName,
                    a.ActionKey,
                    a.ActionName,
                    UserId = ""
                };

            if (!isSuperAdmin)
            {
                query =
                    from up in _context.UserPermissions
                    join p in _context.Permissions on up.PermissionId equals p.PermissionId
                    join m in _context.Modules on p.ModuleId equals m.ModuleId
                    join a in _context.Actions on p.ActionId equals a.ActionId
                    where up.UserId == userId && m.IsActive
                    select new
                    {
                        m.ModuleKey,
                        m.ModuleName,
                        a.ActionKey,
                        a.ActionName,
                        up.UserId
                    };
            }

            var permissions = await query.ToListAsync();
            return permissions
                .SelectMany(p =>
                {
                    var claims = new List<string>
                    {
                        $"{p.ModuleKey}:{p.ActionKey}",
                        $"{p.ModuleName}:{p.ActionName}",
                        $"{p.ModuleKey}:{p.ActionName}",
                        $"{p.ModuleName}:{p.ActionKey}"
                    };

                    if (IsViewAction(p.ActionKey) || IsViewAction(p.ActionName))
                    {
                        claims.Add(p.ModuleKey);
                        claims.Add(p.ModuleName);
                    }

                    return claims;
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsViewAction(string action)
            => NormalizePermissionText(action).Equals("view", StringComparison.OrdinalIgnoreCase);

        private static bool CanRunWriteAction(string action, IEnumerable<string> permissionClaims, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                return true;
            }

            return action switch
            {
                "create_lead" => HasAnyAddPermission(permissionClaims, "lead", "leads", "lead_management", "leadmanagement"),
                "create_task" => HasAnyAddPermission(permissionClaims, "task", "tasks", "task_management", "taskmanagement"),
                "create_expense" => HasAnyAddPermission(permissionClaims, "expense", "expenses", "expense_management", "expensemanagement"),
                _ => false
            };
        }

        private static bool HasAnyAddPermission(IEnumerable<string> permissionClaims, params string[] moduleAliases)
            => (permissionClaims ?? Enumerable.Empty<string>())
                .Select(NormalizePermissionText)
                .Any(claim =>
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

        private static string GetFriendlyActionName(string action)
            => action switch
            {
                "create_lead" => "create leads",
                "create_task" => "create tasks",
                "create_expense" => "record expenses",
                _ => "perform this action"
            };

        private static string GetFriendlyAccessArea(string table)
            => table switch
            {
                "Leads" or "LeadStatusMaster" or "LeadSourceMaster" or "LeadFollowups" or "LeadFollowupStatus" => "leads",
                "Clients" or "States" or "Cities" => "clients",
                "Quotations" or "QuotationItems" or "QuotationStatusMaster" => "quotations",
                "Orders" or "OrderItems" or "OrderStatusMaster" or "DesignStatusMaster" => "orders",
                "Invoices" or "InvoiceStatuses" or "Payments" => "invoices",
                "Expenses" or "ExpenseCategories" => "expenses",
                "Products" or "UnitTypeMaster" or "TaxCategoryMaster" => "products",
                "Projects" or "ProjectStatusMaster" or "ProjectPriorityMaster" => "projects",
                "TaskSeries" or "TaskOccurrences" or "TaskLists" => "tasks",
                "Teams" or "TeamMembers" => "teams",
                "AspNetUsers" or "AspNetRoles" or "AspNetUserRoles" => "users",
                "Modules" or "Actions" or "Permissions" or "UserPermissions" or "RolePermissions" => "permissions",
                "BankDetails" => "bank details",
                "Settings" => "settings",
                _ => table
            };

        public async Task<List<string>> GetUserAllowedModulesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<string>();

            return await (from up in _context.UserPermissions
                          join p in _context.Permissions on up.PermissionId equals p.PermissionId
                          join mm in _context.Modules on p.ModuleId equals mm.ModuleId
                          join a in _context.Actions on p.ActionId equals a.ActionId
                          where up.UserId == userId && mm.IsActive == true && a.ActionKey.ToLower() == "view"
                          select mm.ModuleKey.ToLower()).Distinct().ToListAsync();
        }
    }
}
