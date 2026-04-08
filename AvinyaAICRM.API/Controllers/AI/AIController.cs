using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Domain.Entities.ErrorLogs;
using AvinyaAICRM.Shared.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AvinyaAICRM.Infrastructure.Persistence;
using System.Diagnostics;

namespace AvinyaAICRM.API.Controllers.AI
{
    [Authorize]
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ICRMQueryService _crmService;
        private readonly IErrorLogService _errorLogService;
        private readonly ILogger<AIController> _logger;

        public AIController(
            IAIService aiService, 
            ICRMQueryService crmService,
            IErrorLogService errorLogService,
            ILogger<AIController> logger) 
        {
            _aiService = aiService;
            _crmService = crmService;
            _errorLogService = errorLogService;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIRequest request)
        {
            try
            {
                // Extract TenantId from claims
                var userId = User.FindFirst("userId")?.Value;
                var tenantIdClaim = User.FindFirst("tenantId")?.Value;
                var isSuperAdmin = User.IsInRole("SuperAdmin");

                if (string.IsNullOrEmpty(tenantIdClaim) && !isSuperAdmin) 
                {
                    return Unauthorized("User is not assigned to a valid tenant.");
                }

                Guid tenantId = Guid.TryParse(tenantIdClaim, out var parsed) ? parsed : Guid.Empty;

                // 1. Fetch User Permissions via service
                var allowedModules = await _crmService.GetUserAllowedModulesAsync(userId ?? "");

                // 2. Process Command (Intent + SQL Generation in ONE call)
                var commandResult = await _crmService.ProcessCommandAsync(request.Message, tenantId, userId ?? "", isSuperAdmin, allowedModules);

                // 3. Handle Create Lead special flow
                if (commandResult.Action == "create_lead")
                {
                    return Ok(new
                    {
                        action = commandResult.Action,
                        message = commandResult.ClarificationMessage,
                        isClarificationRequired = commandResult.IsClarificationRequired,
                        suggestedClients = commandResult.SuggestedClients,
                        suggestions = commandResult.Suggestions,
                        data = new List<object>(),
                        count = 0
                    });
                }

                // 4. Handle any Query Execution (If SQL is present, execute it)
                if (!string.IsNullOrEmpty(commandResult.Sql))
                {
                    List<Dictionary<string, object>> data;

                    // Wrap SQL execution in its own try-catch to handle bad AI-generated SQL gracefully
                    try
                    {
                        data = await _crmService.ExecuteRawSqlAsync(commandResult.Sql, tenantId, userId ?? "", isSuperAdmin);
                    }
                    catch (Exception sqlEx)
                    {
                        _logger.LogError(sqlEx, "[AIController] AI-generated SQL execution failed. SQL: {Sql}", commandResult.Sql);

                        // Log to DB
                        await LogErrorToDatabaseAsync(sqlEx, $"AI SQL Execution Failed | SQL: {commandResult.Sql}");

                        // Return a friendly message instead of a 500
                        return Ok(new
                        {
                            query = commandResult.Sql,
                            data = new List<object>(),
                            count = 0,
                            message = "Sorry, I generated an invalid query. Could you rephrase your question?",
                            suggestions = new List<string> { "Show my latest leads", "List pending tasks", "Show today's followups" }
                        });
                    }

                    // Hydrate the template with data from the first row (for reports) or just {count}
                    var finalMessage = commandResult.SuccessMessage ?? "Here is what I found:";
                    
                    if (data.Count > 0)
                    {
                        finalMessage = finalMessage.Replace("{count}", data.Count.ToString());

                        // Support for complex reports: replace {FieldName} or {{FieldName}} with data from the first row
                        var firstRow = data[0];
                        foreach (var kvp in firstRow)
                        {
                            var valueStr = kvp.Value?.ToString() ?? "0";
                            
                            // Replace {{FieldName}}
                            finalMessage = finalMessage.Replace("{{" + kvp.Key + "}}", valueStr);
                            
                            // Replace {FieldName} (single brace)
                            finalMessage = finalMessage.Replace("{" + kvp.Key + "}", valueStr);
                        }
                    }
                    else
                    {
                        finalMessage = commandResult.ErrorMessage ?? "No records found.";
                    }
                    return Ok(new
                    {
                        query = commandResult.Sql, 
                        data = data,
                        count = data.Count,
                        message = finalMessage,
                        suggestions = commandResult.Suggestions
                    });
                }

                // 5. Default Response (Message or Fallback)
                return Ok(new
                {
                    query = "",
                    data = new List<object>(),
                    count = 0,
                    message = commandResult.ClarificationMessage ?? commandResult.ErrorMessage ?? "I'm not sure how to help with that. Can you rephrase it?",
                    suggestions = commandResult.Suggestions
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AIController] Unhandled exception in Chat endpoint");
                await LogErrorToDatabaseAsync(ex);

                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Logs an exception to the ErrorLogs table. Failures are swallowed to prevent cascading errors.
        /// </summary>
        private async System.Threading.Tasks.Task LogErrorToDatabaseAsync(Exception ex, string? additionalContext = null)
        {
            try
            {
                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);

                await _errorLogService.LogAsync(new ErrorLogs
                {
                    Message = string.IsNullOrEmpty(additionalContext) ? ex.Message : $"{additionalContext} | {ex.Message}",
                    Method = frame?.GetMethod()?.Name ?? string.Empty,
                    FileName = frame?.GetFileName() ?? string.Empty,
                    LineNumber = frame?.GetFileLineNumber() ?? 0,
                    Path = HttpContext.Request.Path,
                    StackTrace = ex.StackTrace
                });
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "[AIController] Failed to write error log to database.");
            }
        }
    }
}
