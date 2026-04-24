using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.DTOs.Tasks;
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

        public CRMQueryService(
            AppDbContext context,
            IAIService aiService,
            ILeadService leadService,
            ITaskService taskService,
            AIPipeline pipeline,
            ICreditService credits,
            IAIKnowledgeService knowledge,
            IClientRepository clientRepo)
        {
            _context = context;
            _aiService = aiService;
            _leadService = leadService;
            _taskService = taskService;
            _pipeline = pipeline;
            _credits = credits;
            _knowledge = knowledge;
            _clientRepo = clientRepo;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, bool isSuperAdmin, string userId = "")
        {
            return await ExecuteRawSqlWithHealingAsync(sql, tenantId, isSuperAdmin, "", 2, userId);
        }

        private async Task<List<Dictionary<string, object>>> ExecuteRawSqlWithHealingAsync(string sql, Guid tenantId, bool isSuperAdmin, string originalMessage, int maxRetries, string userId = "")
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
                            if (attempt < maxRetries && !string.IsNullOrWhiteSpace(originalMessage))
                            {
                                currentSql = await _aiService.FixSqlAsync(
                                    currentSql,
                                    "Security Error: Query must be filtered by TenantId.",
                                    originalMessage,
                                    tenantId,
                                    isSuperAdmin);

                                if (!string.IsNullOrWhiteSpace(currentSql))
                                {
                                    continue;
                                }
                            }

                            throw new UnauthorizedAccessException("Security Error: Query must be filtered by TenantId.");
                        }
                    }

                    using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString()))
                    {
                        var queryParams = new { TenantId = tenantId, UserId = userId };
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
                catch (UnauthorizedAccessException)
                {
                    throw;
                }
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

        public async Task<AIResponse> ProcessChatRequestAsync(AIRequest request, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules)
        {
            // 0. Ensure credits record exists for the user
            await _credits.EnsureUserCreditExistsAsync(userId, tenantId);

            // 1. Process Command (Intent + SQL Generation)
            var response = await ProcessCommandAsync(request.Message, tenantId, userId, isSuperAdmin, allowedModules, request.History);

            // 3. Handle Execution
            if (!string.IsNullOrEmpty(response.Sql))
            {
                try 
                {
                    var data = await ExecuteRawSqlWithHealingAsync(response.Sql, tenantId, isSuperAdmin, request.Message, 2, userId);
                    response.Data = data;
                    response.Count = data.Count;
                    response.Query = response.Sql;

                    // 3b. Automatic Knowledge Recording (First time queries)
                    if (response.Source == "ai_sql")
                    {
                        await _knowledge.RecordFirstTimeQueryAsync(request.Message, response.Sql, userId);
                    }

                    // 4. Use Success Message from AI or Knowledge base
                    var finalMessage = response.SuccessMessage ?? "I've found {count} results for you based on the database records.";
                    
                    if (data.Count == 0)
                    {
                        finalMessage = "I couldn't find any records matching your request.";
                    }
                    else
                    {
                        finalMessage = finalMessage.Replace("{count}", data.Count.ToString()).Replace("[count]", data.Count.ToString());
                        finalMessage = FormatMessageWithData(finalMessage, data[0]);
                    }
                    response.Message = finalMessage;
                }
                catch (Exception ex)
                {
                    response.ErrorMessage = "Query execution failed: " + ex.Message;
                    response.Message = response.ErrorMessage;
                }
            }
            else if (response.Action == "create_lead")
            {
                try
                {
                    // 1. Validate required parameters (AI should have filled these)
                    var companyName = response.Parameters?.ContainsKey("CompanyName") == true ? response.Parameters["CompanyName"]?.ToString() : null;
                    var requirements = response.Parameters?.ContainsKey("RequirementDetails") == true ? response.Parameters["RequirementDetails"]?.ToString() : null;

                    if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(requirements))
                    {
                        response.Message = "I need a company name and requirement details to create a lead. " + (response.ErrorMessage ?? "");
                        return response;
                    }

                    // 2. Extract and fill LeadRequestDto
                    var dto = new LeadRequestDto
                    {
                        CompanyName = companyName,
                        RequirementDetails = requirements,
                        ContactPerson = response.Parameters.ContainsKey("ContactPerson") ? response.Parameters["ContactPerson"]?.ToString() : null,
                        Mobile = response.Parameters.ContainsKey("Mobile") ? response.Parameters["Mobile"]?.ToString() : null,
                        Email = response.Parameters.ContainsKey("Email") ? response.Parameters["Email"]?.ToString() : null,
                        GSTNo = response.Parameters.ContainsKey("GSTNo") ? response.Parameters["GSTNo"]?.ToString() : null,
                        BillingAddress = response.Parameters.ContainsKey("BillingAddress") ? response.Parameters["BillingAddress"]?.ToString() : null,
                        OtherSources = response.Parameters.ContainsKey("OtherSources") ? response.Parameters["OtherSources"]?.ToString() : null,
                        Notes = response.Parameters.ContainsKey("Notes") ? response.Parameters["Notes"]?.ToString() : null,
                        Links = response.Parameters.ContainsKey("Links") ? response.Parameters["Links"]?.ToString() : null,
                    };

                    if (response.Parameters.TryGetValue("ClientType", out var cType))
                    {
                        dto.ClientType = cType.Equals("Individual", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
                    }

                    // --- RESOLVE NAMES TO IDs ---
                    
                    // 1. Resolve State & City
                    if (response.Parameters.TryGetValue("StateID", out var stateName) && !string.IsNullOrEmpty(stateName))
                    {
                        var state = await _context.States.FirstOrDefaultAsync(s => s.StateName.Contains(stateName));
                        if (state != null) dto.StateID = state.StateID;
                    }
                    if (response.Parameters.TryGetValue("CityID", out var cityName) && !string.IsNullOrEmpty(cityName))
                    {
                        var city = await _context.Cities.FirstOrDefaultAsync(c => c.CityName.Contains(cityName));
                        if (city != null) 
                        {
                            dto.CityID = city.CityID;
                            if (dto.StateID == null) dto.StateID = city.StateID; // Auto-set state if city found
                        }
                    }

                    // 2. Resolve Lead Source & Status
                    if (response.Parameters.TryGetValue("LeadSourceID", out var sourceName) && !string.IsNullOrEmpty(sourceName))
                    {
                        var source = await _context.leadSourceMasters.FirstOrDefaultAsync(s => s.SourceName.Contains(sourceName) && s.IsActive);
                        if (source != null) dto.LeadSourceID = source.LeadSourceID;
                    }
                    if (response.Parameters.TryGetValue("LeadStatusID", out var statusName) && !string.IsNullOrEmpty(statusName))
                    {
                        var status = await _context.leadStatusMasters.FirstOrDefaultAsync(s => s.StatusName.Contains(statusName) && s.IsActive);
                        if (status != null) dto.LeadStatusID = status.LeadStatusID;
                    }

                    // 3. Resolve AssignedTo (User)
                    if (response.Parameters.TryGetValue("AssignedTo", out var userName) && !string.IsNullOrEmpty(userName))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName.Contains(userName) && u.TenantId == tenantId);
                        if (user != null) dto.AssignedTo = user.Id;
                        else dto.AssignedTo = userName; // Fallback to raw value if not found
                    }

                    // 4. Handle Dates
                    if (response.Parameters.TryGetValue("NextFollowupDate", out var dateStr) && DateTime.TryParse(dateStr, out var followDate)) 
                        dto.NextFollowupDate = followDate;



                    // 3. Search for existing client
                    var existingClients = await _clientRepo.FindByNameAsync(companyName, tenantId);
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
                    }

                    // 4. Create Lead
                    var result = await _leadService.CreateAsync(dto, userId);
                    if (result.StatusCode == 200)
                    {
                        response.Message = !string.IsNullOrWhiteSpace(response.SuccessMessage) 
                            ? response.SuccessMessage 
                            : $"I've successfully created the lead for {companyName}.";
                    }
                    else
                    {
                        response.ErrorMessage = result.StatusMessage;
                        response.Message = "I tried to create the lead but failed: " + result.StatusMessage;
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
                    if (DateTime.TryParse(dueDateStr, out var dueDate)) dto.DueDateTime = dueDate;

                    // 4. Resolve List
                    if (response.Parameters.TryGetValue("ListName", out var listName) && !string.IsNullOrEmpty(listName))
                    {
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
                    if (response.Parameters.TryGetValue("AssignToName", out var userName) && !string.IsNullOrEmpty(userName))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName.Contains(userName) && u.TenantId == tenantId);
                        if (user != null) dto.AssignToId = user.Id;
                    }

                    // 7. Resolve Project
                    if (response.Parameters.TryGetValue("ProjectName", out var projectName) && !string.IsNullOrEmpty(projectName))
                    {
                        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName.Contains(projectName) && p.TenantId == tenantId);
                        if (project != null) dto.ProjectId = project.ProjectID.ToString();
                    }

                    // 8. Handle Reminders and Recurrence
                    if (response.Parameters.TryGetValue("ReminderAt", out var reminderStr) && DateTime.TryParse(reminderStr, out var reminderDate)) 
                        dto.ReminderAt = reminderDate;
                    
                    if (response.Parameters.TryGetValue("IsRecurring", out var recurStr) && bool.TryParse(recurStr, out var isRecur)) 
                        dto.IsRecurring = isRecur;
                    
                    if (response.Parameters.TryGetValue("RecurrenceRule", out var rRule)) 
                        dto.RecurrenceRule = rRule;

                    // 9. Create Task
                    var result = await _taskService.CreateTaskAsync(dto, userId);
                    if (result.StatusCode == 200)
                    {
                        response.Message = !string.IsNullOrWhiteSpace(response.SuccessMessage) 
                            ? response.SuccessMessage 
                            : $"I've successfully created the task: {title}.";
                    }
                    else
                    {
                        response.ErrorMessage = result.StatusMessage;
                        response.Message = "I tried to create the task but failed: " + result.StatusMessage;
                    }
                }
                catch (Exception ex)
                {
                    response.ErrorMessage = "Task creation failed: " + ex.Message;
                    response.Message = response.ErrorMessage;
                }
            }
            else if (response.Action == "message")
            {
                SetEmptyResponse(response, response.SuccessMessage ?? response.Message ?? "I'm here to help! What can I do for you?");
            }
            else 
            {
                SetEmptyResponse(response, response.SuccessMessage ?? response.ClarificationMessage ?? response.ErrorMessage ?? "I'm not sure how to help with that. Could you please rephrase?");
            }

            return response;
        }

        private static void SetEmptyResponse(AIResponse response, string message)
        {
            response.Message = message;
            response.Data = new List<Dictionary<string, object>>();
            response.Count = 0;
        }

        private string FormatMessageWithData(string template, Dictionary<string, object> row)
        {
            var formatted = template;
            var displayValues = new Dictionary<string, string>();

            foreach (var kvp in row)
            {
                string val = kvp.Value?.ToString() ?? "";
                
                if (kvp.Value is DateTime dt)
                {
                    val = dt.ToString("dd MMM yyyy HH:mm");
                }
                else if (kvp.Key.Contains("Date") || kvp.Key.Contains("Time"))
                {
                    if (DateTime.TryParse(val, out var dtParsed))
                        val = dtParsed.ToString("dd MMM yyyy HH:mm");
                }

                if (string.IsNullOrEmpty(val) || val == "0") val = "N/A";
                displayValues[kvp.Key] = val;
            }

            // Handle JsonResult (Dashboard/360)
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
                            {
                                formatted = ReplaceTemplateToken(formatted, kvp.Key, kvp.Value?.ToString() ?? "0");
                            }
                        }
                    } catch { /* Ignore malformed JSON */ }
                }
            }

            // Replace standard placeholders
            foreach (var kvp in displayValues)
            {
                formatted = ReplaceTemplateToken(formatted, kvp.Key, kvp.Value);
            }

            return formatted;
        }

        private static string ReplaceTemplateToken(string template, string key, string value)
            => template.Replace("{" + key + "}", value).Replace("[" + key + "]", value);

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
                RemainingCredits = pipelineResult.RemainingCredits,
                Source = pipelineResult.Source,
                Suggestions = pipelineResult.Suggestions
            };

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
