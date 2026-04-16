using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Domain.Enums.Clients;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.AI;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace AvinyaAICRM.Application.Services.AICHATS
{
    public class CRMQueryService : ICRMQueryService
    {
        private readonly AppDbContext _context;
        private readonly ILeadService _leadService;
        private readonly ITaskService _taskService;
        private readonly ITeamRepository _teamRepo;
        private readonly ICreditService _creditService;
        private readonly IDynamicQueryBuilder _queryBuilder;
        private readonly IAIService _aiService;

        public CRMQueryService(
            AppDbContext context, 
            IAIService aiService, 
            ILeadService leadService, 
            ITaskService taskService,
            ITeamRepository teamRepo,
            ICreditService creditService,
            IDynamicQueryBuilder queryBuilder)
        {
            _context = context;
            _aiService = aiService;
            _leadService = leadService;
            _taskService = taskService;
            _teamRepo = teamRepo;
            _creditService = creditService;
            _queryBuilder = queryBuilder;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, string userId, bool isAdmin, Dictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(sql)) return new List<Dictionary<string, object>>();

            // Basic safety check (Server side)
            var forbidden = new[] { "UPDATE", "DELETE", "DROP", "INSERT", "ALTER", "TRUNCATE", "CREATE" };
            if (!isAdmin && sql.ToUpper().Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Any(word => forbidden.Contains(word)))
            {
                throw new UnauthorizedAccessException("Only SELECT queries are allowed for safety.");
            }

            using (var connection = _context.Database.GetDbConnection())
            {
                var queryParams = parameters ?? new Dictionary<string, object>();
                queryParams["TenantId"] = tenantId;
                queryParams["UserId"] = userId;
                
                // Security Guard: 30 second timeout
                var results = await connection.QueryAsync(sql, queryParams, commandTimeout: 30);

                // Convert DapperRow to Dictionary<string, object> for the UI
                var data = results.Select(x => (Dictionary<string, object>)new Dictionary<string, object>((IDictionary<string, object>)x)).ToList();

                // Architect Refinement: Empty Result Fallback
                if (!data.Any())
                {
                    data.Add(new Dictionary<string, object>
                    {
                        ["Message"] = "No data found for your request.",
                        ["Suggestion"] = "Try a different date range or remove filters."
                    });
                }

                return data;
            }
        }

        public async Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isAdmin, List<string> allowedModules)
        {
            // 0. Ensure Credit Record exists
            await _creditService.EnsureUserCreditExistsAsync(userId, tenantId);

            // 1. Credit Check (Pre-emptive)
            if (!await _creditService.HasEnoughCreditsAsync(userId, 1))
            {
                return new AIResponse { Action = "message", ErrorMessage = "You don't have enough credits to use the chatbot. Please recharge." };
            }

            // 2. AI Analysis (Intent + Filters)
            var aiResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isAdmin, allowedModules);

            if (aiResponse.Action == "create_lead")
            {
                return await HandleCreateLeadAsync(aiResponse, tenantId, userId, isAdmin);
            }
            
            if (aiResponse.Action == "create_task")
            {
                aiResponse = await HandleCreateTaskAsync(aiResponse, tenantId, userId, isAdmin);
            }

            if (aiResponse.Action == "get_data")
            {
                try 
                {
                    // 3. Normalize & Build Query
                    var request = _queryBuilder.NormalizeRequest(aiResponse);
                    var (sql, parameters) = _queryBuilder.BuildQuery(request, tenantId, userId, isAdmin, allowedModules);
                    
                    aiResponse.Sql = sql;
                    aiResponse.SqlQueryParameters = parameters;
                    
                    // 4. Execution (Handled by Controller/Client via returned SQL, 
                    // or we could execute here if we wanted to return data directly.
                    // Assuming for this CRM design, the frontend calls ExecuteRawSqlAsync with the returned SQL.)
                    
                    // 5. Credit Deduction (Post-success logic)
                    int cost = request.Type == QueryType.SUMMARY ? 2 : (request.Entities?.Count > 1 ? 3 : 1);
                    await _creditService.DeductCreditsAsync(userId, cost, aiResponse.Action.ToUpper());
                    
                    aiResponse.SuccessMessage = $"Query generated successfully. (Cost: {cost} credits)";
                }
                catch (Exception ex)
                {
                    aiResponse.ErrorMessage = "Error generating query: " + ex.Message;
                }
            }

            // Generate suggestions based on the result
            aiResponse.Suggestions = GenerateSuggestions(aiResponse, allowedModules);

            return aiResponse;
        }

        private List<string> GenerateSuggestions(AIResponse response, List<string> allowedModules)
        {
            var suggestions = response.Suggestions ?? new List<string>();
            var action = response.Action?.ToLower();
            var sql = response.Sql?.ToUpper() ?? "";

            if (response.IsClarificationRequired)
            {
                return suggestions;
            }

            // 1. Contextual Suggestions based on SQL Tables
            if (!string.IsNullOrEmpty(sql))
            {
                if (sql.Contains("LEADFOLLOWUPS"))
                {
                    suggestions.Add("Show my followups for today");
                    if (allowedModules.Any(m => m.Contains("lead"))) suggestions.Add("Show latest leads");
                    return suggestions;
                }
                if (sql.Contains("LEADS"))
                {
                    suggestions.Add("Show all leads added today");
                    if (allowedModules.Any(m => m.Contains("task"))) suggestions.Add("Schedule a follow-up task");
                    return suggestions;
                }
                if (sql.Contains("TASKS"))
                {
                    suggestions.Add("List my pending tasks");
                    suggestions.Add("Show tasks for tomorrow");
                    return suggestions;
                }
                if (sql.Contains("INVOICES"))
                {
                    suggestions.Add("Show my unpaid invoices");
                    suggestions.Add("List total outstanding amount");
                    return suggestions;
                }
                if (sql.Contains("PAYMENTS"))
                {
                    suggestions.Add("Show latest payments");
                    suggestions.Add("How much payment received today?");
                    return suggestions;
                }
            }

            // 2. Fallback Suggestions based on Action or General Onboarding
            switch (action)
            {
                case "create_lead":
                    suggestions.Add("Show all leads added today");
                    if (allowedModules.Any(m => m.Contains("task"))) suggestions.Add("Schedule a follow-up for this lead");
                    break;

                case "create_task":
                    suggestions.Add("Show my tasks for tomorrow");
                    suggestions.Add("List all pending team tasks");
                    break;

                case "message":
                default:
                    // General onboarding
                    if (allowedModules.Any(m => m.Contains("lead"))) suggestions.Add("Show my latest leads");
                    if (allowedModules.Any(m => m.Contains("task"))) suggestions.Add("List upcoming meetings");
                    if (allowedModules.Any(m => m.Contains("project"))) suggestions.Add("What's the status of current projects?");
                    if (allowedModules.Any(m => m.Contains("invoice"))) suggestions.Add("Show total outstanding balance");
                    if (allowedModules.Any(m => m.Contains("payment"))) suggestions.Add("Show recent payments received");
                    break;
            }

            return suggestions;
        }

        private async Task<AIResponse> HandleCreateTaskAsync(AIResponse aiResponse, Guid tenantId, string userId, bool isAdmin)
        {
            if (aiResponse.IsClarificationRequired) return aiResponse;

            var p = aiResponse.Parameters ?? new Dictionary<string, object>();
            p.TryGetValue("TaskScope", out var scopeObj);
            var scope = scopeObj?.ToString();

            p.TryGetValue("TeamName", out var teamNameObj);
            var teamName = teamNameObj?.ToString();

            long? teamId = null;
            if (scope?.ToLower() == "team")
            {
                teamId = await _teamRepo.ResolveTeamId(userId, teamName);
                if (teamId == null && !string.IsNullOrEmpty(teamName))
                {
                    aiResponse.IsClarificationRequired = true;
                    aiResponse.ClarificationMessage = $"I couldn't find a team named '{teamName}'. Please check the team name or choose one from your managed teams.";
                    return aiResponse;
                }
                else if (teamId == null && string.IsNullOrEmpty(teamName))
                {
                    aiResponse.IsClarificationRequired = true;
                    aiResponse.ClarificationMessage = "This is a team task, but you didn't specify which team. Which team should I assign this to?";
                    return aiResponse;
                }
            }

            // Resolve Assignee if name provided
            string? assignToId = null;
            p.TryGetValue("AssignToName", out var assignToNameObj);
            var assignToName = assignToNameObj?.ToString();
            if (!string.IsNullOrEmpty(assignToName))
            {
                var user = await _context.Users
                    .Where(u => u.FullName.Contains(assignToName) && (isAdmin || u.TenantId == tenantId))
                    .FirstOrDefaultAsync();
                
                assignToId = user?.Id;
            }

            DateTime? dueDate = null;
            if (p.TryGetValue("DueDateTime", out var dueObj) && DateTime.TryParse(dueObj?.ToString(), out var d)) dueDate = d;

            DateTime? reminderAt = null;
            if (p.TryGetValue("ReminderAt", out var remObj) && DateTime.TryParse(remObj?.ToString(), out var r)) reminderAt = r;

            var dto = new CreateTaskDto
            {
                Title = p.GetValueOrDefault("Title")?.ToString() ?? "New Task",
                Description = p.GetValueOrDefault("Description")?.ToString(),
                Notes = p.GetValueOrDefault("Notes")?.ToString(),
                AssignToId = assignToId ?? userId,
                DueDateTime = dueDate,
                ReminderAt = reminderAt,
                ReminderChannel = "in-app", // Default for chatbot
                TeamId = teamId,
                Scope = scope ?? "Personal",
                IsRecurring = p.TryGetValue("IsRecurring", out var rec) && bool.TryParse(rec?.ToString(), out var isRec) && isRec,
                RecurrenceRule = p.GetValueOrDefault("RecurrenceRule")?.ToString(),
                ListId = 0 // Default list
            };

            var result = await _taskService.CreateTaskAsync(dto, userId);

            if (result.StatusCode == 200 || result.StatusCode == 201)
            {
                aiResponse.ClarificationMessage = $"Successfully created the task: {dto.Title}.";
                if (dueDate.HasValue) aiResponse.ClarificationMessage += $" Due on {dueDate.Value:f}.";
                if (teamId.HasValue) aiResponse.ClarificationMessage += $" (Team Task)";
            }
            else
            {
                aiResponse.ClarificationMessage = "Error creating task: " + result.StatusMessage;
            }

            return aiResponse;
        }

        private async Task<AIResponse> HandleCreateLeadAsync(AIResponse aiResponse, Guid tenantId, string userId, bool isAdmin)
        {
            var parameters = aiResponse.Parameters ?? new Dictionary<string, object>();
            parameters.TryGetValue("CompanyName", out var clientNameObj);
            var clientName = clientNameObj?.ToString();

            if (string.IsNullOrEmpty(clientName))
            {
                aiResponse.IsClarificationRequired = true;
                aiResponse.ClarificationMessage = "To create a lead, I need at least a Company or Client Name. Who is this lead for?";
                return aiResponse;
            }

            // check if it's already a client
            var existingClients = await _context.Clients
                .Where(c => c.CompanyName.Contains(clientName) && (isAdmin || c.TenantId == tenantId))
                .Select(c => new ClientDisambiguationDto
                {
                    ClientID = c.ClientID,
                    CompanyName = c.CompanyName,
                    Email = c.Email,
                    Mobile = c.Mobile
                })
                .ToListAsync();

            if (existingClients.Count == 0)
            {
                return await CreateLeadAndFinalize(aiResponse, null, tenantId, userId);
            }
            else if (existingClients.Count == 1)
            {
                return await CreateLeadAndFinalize(aiResponse, existingClients[0].ClientID, tenantId, userId);
            }
            else
            {
                parameters.TryGetValue("Email", out var emailObj);
                var email = emailObj?.ToString();

                parameters.TryGetValue("Mobile", out var mobileObj);
                var mobile = mobileObj?.ToString();

                if (!string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(mobile))
                {
                    var exactMatch = existingClients.FirstOrDefault(c => 
                        (!string.IsNullOrEmpty(email) && c.Email == email) || 
                        (!string.IsNullOrEmpty(mobile) && c.Mobile == mobile));

                    if (exactMatch != null)
                    {
                        return await CreateLeadAndFinalize(aiResponse, exactMatch.ClientID, tenantId, userId);
                    }
                }

                aiResponse.IsClarificationRequired = true;
                aiResponse.SuggestedClients = existingClients;
                aiResponse.ClarificationMessage = $"I found multiple clients named '{clientName}'. Please provide an email or phone number to identify the correct one, or select from the list below:";
                return aiResponse;
            }
        }

        private async Task<AIResponse> CreateLeadAndFinalize(AIResponse aiResponse, Guid? clientId, Guid tenantId, string userId)
        {
            var p = aiResponse.Parameters ?? new Dictionary<string, object>();
            
            p.TryGetValue("ClientType", out var clientTypeStrObj);
            var clientTypeStr = clientTypeStrObj?.ToString();
            var clientType = (clientTypeStr?.ToLower() == "individual") ? (int)ClientTypeEnum.Individual : (int)ClientTypeEnum.Company;

            var dto = new LeadRequestDto
            {
                ClientID = clientId,
                CompanyName = p.GetValueOrDefault("CompanyName")?.ToString(),
                Email = p.GetValueOrDefault("Email")?.ToString(),
                Mobile = p.GetValueOrDefault("Mobile")?.ToString(),
                Notes = p.GetValueOrDefault("Notes")?.ToString(),
                ContactPerson = p.GetValueOrDefault("ContactPerson")?.ToString(),
                RequirementDetails = p.GetValueOrDefault("Notes")?.ToString(),
                Date = DateTime.UtcNow,
                CreatedBy = userId,
                ClientType = clientType
            };

            var result = await _leadService.CreateAsync(dto, userId);

            if (result.StatusCode == 200 || result.StatusCode == 201)
            {
                aiResponse.ClarificationMessage = clientId.HasValue 
                    ? $"Successfully created lead for existing client: {dto.CompanyName}." 
                    : $"Successfully created a new lead for: {dto.CompanyName}.";
            }
            else
            {
                aiResponse.ClarificationMessage = "I encountered an error while creating the lead: " + result.StatusMessage;
            }

            return aiResponse;
        }

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
