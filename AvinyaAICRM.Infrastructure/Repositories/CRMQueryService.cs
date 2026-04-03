using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Domain.Enums.Clients;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
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
        private readonly IAIService _aiService;
        private readonly ILeadService _leadService;
        private readonly ITaskService _taskService;
        private readonly ITeamRepository _teamRepo;

        public CRMQueryService(
            AppDbContext context, 
            IAIService aiService, 
            ILeadService leadService, 
            ITaskService taskService,
            ITeamRepository teamRepo)
        {
            _context = context;
            _aiService = aiService;
            _leadService = leadService;
            _taskService = taskService;
            _teamRepo = teamRepo;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, string userId, bool isSuperAdmin)
        {
            if (string.IsNullOrWhiteSpace(sql)) return new List<Dictionary<string, object>>();

            // Basic safety check (Server side)
            var forbidden = new[] { "UPDATE", "DELETE", "DROP", "INSERT", "ALTER", "TRUNCATE", "CREATE" };
            if (!isSuperAdmin && sql.ToUpper().Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Any(word => forbidden.Contains(word)))
            {
                throw new UnauthorizedAccessException("Only SELECT queries are allowed for safety.");
            }

            // Security Guard: For regular users, the AI must have included the @TenantId parameter
            if (!isSuperAdmin)
            {
                if (!sql.Contains("@TenantId", StringComparison.OrdinalIgnoreCase) && !sql.Contains(tenantId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Security Error: Query must be filtered by TenantId.");
                }
            }

            using (var connection = _context.Database.GetDbConnection())
            {
                var queryParams = new { TenantId = tenantId, CurrentUserId = userId };
                
                // Dapper handles the mapping to dynamic (DapperRow) automatically
                var results = await connection.QueryAsync(sql, queryParams);

                // Convert DapperRow to Dictionary<string, object> for the UI
                return results.Select(x => (Dictionary<string, object>)new Dictionary<string, object>((IDictionary<string, object>)x)).ToList();
            }
        }

        public async Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules)
        {
            // 1. Unified Analysis (Intent + Data + SQL if summary)
            var aiResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isSuperAdmin, allowedModules);

            if (aiResponse.Action == "create_lead")
            {
                return await HandleCreateLeadAsync(aiResponse, tenantId, userId, isSuperAdmin);
            }
            
            if (aiResponse.Action == "create_task")
            {
                aiResponse = await HandleCreateTaskAsync(aiResponse, tenantId, userId, isSuperAdmin);
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
                    break;
            }

            return suggestions;
        }

        private async Task<AIResponse> HandleCreateTaskAsync(AIResponse aiResponse, Guid tenantId, string userId, bool isSuperAdmin)
        {
            if (aiResponse.IsClarificationRequired) return aiResponse;

            var p = aiResponse.Parameters ?? new Dictionary<string, string>();
            p.TryGetValue("TaskScope", out var scope);
            p.TryGetValue("TeamName", out var teamName);

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
            p.TryGetValue("AssignToName", out var assignToName);
            if (!string.IsNullOrEmpty(assignToName))
            {
                var user = await _context.Users
                    .Where(u => u.FullName.Contains(assignToName) && (isSuperAdmin || u.TenantId == tenantId))
                    .FirstOrDefaultAsync();
                
                assignToId = user?.Id;
            }

            DateTime? dueDate = null;
            if (p.TryGetValue("DueDateTime", out var dueStr) && DateTime.TryParse(dueStr, out var d)) dueDate = d;

            DateTime? reminderAt = null;
            if (p.TryGetValue("ReminderAt", out var remStr) && DateTime.TryParse(remStr, out var r)) reminderAt = r;

            var dto = new CreateTaskDto
            {
                Title = p.GetValueOrDefault("Title") ?? "New Task",
                Description = p.GetValueOrDefault("Description"),
                Notes = p.GetValueOrDefault("Notes"),
                AssignToId = assignToId ?? userId,
                DueDateTime = dueDate,
                ReminderAt = reminderAt,
                ReminderChannel = "in-app", // Default for chatbot
                TeamId = teamId,
                Scope = scope ?? "Personal",
                IsRecurring = p.TryGetValue("IsRecurring", out var rec) && bool.TryParse(rec, out var b) && b,
                RecurrenceRule = p.GetValueOrDefault("RecurrenceRule"),
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

        private async Task<AIResponse> HandleCreateLeadAsync(AIResponse aiResponse, Guid tenantId, string userId, bool isSuperAdmin)
        {
            var parameters = aiResponse.Parameters ?? new Dictionary<string, string>();
            parameters.TryGetValue("CompanyName", out var clientName);

            if (string.IsNullOrEmpty(clientName))
            {
                aiResponse.IsClarificationRequired = true;
                aiResponse.ClarificationMessage = "To create a lead, I need at least a Company or Client Name. Who is this lead for?";
                return aiResponse;
            }

            // check if it's already a client
            var existingClients = await _context.Clients
                .Where(c => c.CompanyName.Contains(clientName) && (isSuperAdmin || c.TenantId == tenantId))
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
                parameters.TryGetValue("Email", out var email);
                parameters.TryGetValue("Mobile", out var mobile);

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
            var p = aiResponse.Parameters ?? new Dictionary<string, string>();
            
            p.TryGetValue("ClientType", out var clientTypeStr);
            var clientType = (clientTypeStr?.ToLower() == "individual") ? (int)ClientTypeEnum.Individual : (int)ClientTypeEnum.Company;

            var dto = new LeadRequestDto
            {
                ClientID = clientId,
                CompanyName = p.GetValueOrDefault("CompanyName"),
                Email = p.GetValueOrDefault("Email"),
                Mobile = p.GetValueOrDefault("Mobile"),
                Notes = p.GetValueOrDefault("Notes"),
                ContactPerson = p.GetValueOrDefault("ContactPerson"),
                RequirementDetails = p.GetValueOrDefault("Notes"),
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
