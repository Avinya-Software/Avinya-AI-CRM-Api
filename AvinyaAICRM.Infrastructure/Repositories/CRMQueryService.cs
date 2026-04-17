using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.AI;
using Dapper;
using Microsoft.EntityFrameworkCore;

// Existing using block remains plus these:
using AvinyaAICRM.Application.AI.Pipeline;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;

namespace AvinyaAICRM.Application.Services.AICHATS
{
    public class CRMQueryService : ICRMQueryService
    {
        private readonly AppDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILeadService _leadService;
        private readonly ITaskService _taskService;
        private readonly ITeamRepository _teamRepo;
        private readonly AIPipeline _pipeline;
        private readonly ILeadFlowService _leadFlow;
        private readonly ICreditService _credits;

        public CRMQueryService(
            AppDbContext context,
            IAIService aiService,
            ILeadService leadService,
            ITaskService taskService,
            ITeamRepository teamRepo,
            AIPipeline pipeline,
            ILeadFlowService leadFlow,
            ICreditService credits)
        {
            _context = context;
            _aiService = aiService;
            _leadService = leadService;
            _taskService = taskService;
            _teamRepo = teamRepo;
            _pipeline = pipeline;
            _leadFlow = leadFlow;
            _credits = credits;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, bool isSuperAdmin)
        {
            return await ExecuteRawSqlWithHealingAsync(sql, tenantId, isSuperAdmin, "", 2);
        }

        private async Task<List<Dictionary<string, object>>> ExecuteRawSqlWithHealingAsync(string sql, Guid tenantId, bool isSuperAdmin, string originalMessage, int maxRetries)
        {
            if (string.IsNullOrWhiteSpace(sql)) return new List<Dictionary<string, object>>();

            string currentSql = sql;
            Exception lastError = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Basic safety check (Server side)
                    var forbidden = new[] { "UPDATE", "DELETE", "DROP", "INSERT", "ALTER", "TRUNCATE", "CREATE" };
                    if (!isSuperAdmin && currentSql.ToUpper().Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Any(word => forbidden.Contains(word)))
                    {
                        throw new UnauthorizedAccessException("Only SELECT queries are allowed for safety.");
                    }

                    // Security Guard
                    if (!isSuperAdmin)
                    {
                        if (!currentSql.Contains("@TenantId", StringComparison.OrdinalIgnoreCase) && !currentSql.Contains(tenantId.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new UnauthorizedAccessException("Security Error: Query must be filtered by TenantId.");
                        }
                    }

                    using (var connection = _context.Database.GetDbConnection())
                    {
                        var queryParams = new { TenantId = tenantId };
                        var results = (await connection.QueryAsync(currentSql, queryParams)).ToList();

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

                        return results.Select(x => (Dictionary<string, object>)new Dictionary<string, object>((IDictionary<string, object>)x)).ToList();
                    }
                }
                catch (UnauthorizedAccessException) { throw; }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt < maxRetries && !string.IsNullOrEmpty(originalMessage))
                    {
                        currentSql = await _aiService.FixSqlAsync(currentSql, ex.Message, originalMessage, tenantId, isSuperAdmin);
                        if (string.IsNullOrEmpty(currentSql)) break;
                    }
                }
            }

            throw lastError ?? new Exception("Database query failed.");
        }

        public async Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null)
        {
            // 0. Check for Step-by-Step Flow (e.g. Lead Creation)
            var flowResult = await _leadFlow.ProcessFlowAsync(message, tenantId, userId);
            if (flowResult != null)
            {
                if (flowResult.TotalTokens > 0)
                {
                    await _credits.DeductCreditsAsync(userId, flowResult.TotalTokens, "LEAD_FLOW");
                }
                flowResult.RemainingCredits = await _credits.GetRemainingCreditsAsync(userId);
                return flowResult;
            }

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
                RemainingCredits = pipelineResult.RemainingCredits
            };

            if (aiResponse.Action == "create_lead")
            {
                return await HandleCreateLeadAsync(aiResponse, tenantId, userId, isSuperAdmin);
            }

            if (aiResponse.Action == "create_task")
            {
                return await HandleCreateTaskAsync(aiResponse, tenantId, userId, isSuperAdmin);
            }

            return aiResponse;
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
                aiResponse.SuccessMessage = $"Successfully created the task: {dto.Title}.";
                if (dueDate.HasValue) aiResponse.SuccessMessage += $" Due on {dueDate.Value:f}.";
                if (teamId.HasValue) aiResponse.SuccessMessage += $" (Team Task)";
                aiResponse.ClarificationMessage = aiResponse.SuccessMessage;
            }
            else
            {
                aiResponse.ErrorMessage = "Error creating task: " + result.StatusMessage;
                aiResponse.ClarificationMessage = aiResponse.ErrorMessage;
            }

            return aiResponse;
        }

        private async Task<AIResponse> HandleCreateLeadAsync(AIResponse aiResponse, Guid tenantId, string userId, bool isSuperAdmin)
        {
            var parameters = aiResponse.Parameters ?? new Dictionary<string, string>();
            parameters.TryGetValue("CompanyName", out var clientName);

            if (!string.IsNullOrEmpty(clientName))
            {
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

            return await CreateLeadAndFinalize(aiResponse, null, tenantId, userId);
        }

        private async Task<AIResponse> CreateLeadAndFinalize(AIResponse aiResponse, Guid? clientId, Guid tenantId, string userId)
        {
            var p = aiResponse.Parameters ?? new Dictionary<string, string>();

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
                CreatedBy = userId
            };

            var result = await _leadService.CreateAsync(dto, userId);

            if (result.StatusCode == 200 || result.StatusCode == 201)
            {
                aiResponse.SuccessMessage = clientId.HasValue
                    ? $"Successfully created lead for existing client: {dto.CompanyName}."
                    : $"Successfully created a new lead for: {dto.CompanyName}.";
                aiResponse.ClarificationMessage = aiResponse.SuccessMessage;
            }
            else
            {
                aiResponse.ErrorMessage = "I encountered an error while creating the lead: " + result.StatusMessage;
                aiResponse.ClarificationMessage = aiResponse.ErrorMessage;
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