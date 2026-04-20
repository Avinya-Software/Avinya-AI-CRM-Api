using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Shared.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvinyaAICRM.Application.AI.Pipeline;

namespace AvinyaAICRM.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ICRMQueryService _crmService;
        private readonly LocalIntentClassifier _classifier;
        private readonly SqlTemplateEngine _templates;

        public AIController(
            IAIService aiService, 
            ICRMQueryService crmService,
            LocalIntentClassifier classifier,
            SqlTemplateEngine templates)
        {
            _aiService = aiService;
            _crmService = crmService;
            _classifier = classifier;
            _templates = templates;
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
                var commandResult = await _crmService.ProcessCommandAsync(request.Message, tenantId, userId ?? "", isSuperAdmin, allowedModules, request.History);

                // 3. Handle Creation actions (Lead, Task)
                if (commandResult.Action == "create_lead" || commandResult.Action == "create_task")
                {
                    return Ok(new
                    {
                        action = commandResult.Action,
                        message = commandResult.SuccessMessage ?? commandResult.ClarificationMessage ?? commandResult.ErrorMessage,
                        isClarificationRequired = commandResult.IsClarificationRequired,
                        suggestedClients = commandResult.SuggestedClients,
                        data = new List<object>(),
                        count = 0,
                        totalTokens = commandResult.TotalTokens,
                        remainingCredits = commandResult.RemainingCredits
                    });
                }


                // 4. Handle any Query Execution (If SQL is present, execute it)
                if (!string.IsNullOrEmpty(commandResult.Sql))
                {
                    // Execute the query
                    var data = await _crmService.ExecuteRawSqlAsync(commandResult.Sql, tenantId, isSuperAdmin);

                    // 4a. Get Dynamic Template from Engine
                    var filters = _classifier.ExtractFilters(request.Message);
                    var finalMessage = _templates.GetTemplateMessage(commandResult.Intent ?? "unknown", filters, data.Count);

                    if (data.Count > 0)
                    {
                        var firstRow = data[0];

                        // Create a formatted version of values for placeholders
                        var formattedData = new Dictionary<string, string>();
                        foreach (var kvp in firstRow)
                        {
                            string displayValue = kvp.Value?.ToString() ?? "";
                            
                            // Format Dates nicely
                            if (kvp.Value is DateTime dt)
                            {
                                displayValue = dt.ToString("dd MMM yyyy HH:mm");
                            }
                            else if (kvp.Key.Contains("Date") || kvp.Key.Contains("Time"))
                            {
                                if (DateTime.TryParse(displayValue, out var dtParsed))
                                    displayValue = dtParsed.ToString("dd MMM yyyy HH:mm");
                                else if (string.IsNullOrEmpty(displayValue))
                                    displayValue = "Not Scheduled";
                            }

                            if (string.IsNullOrEmpty(displayValue) || displayValue == "0")
                                displayValue = "N/A";

                            formattedData[kvp.Key] = displayValue;
                        }

                        // Special Handling for JSON Result (Dashboard)
                        if (firstRow.ContainsKey("JsonResult"))
                        {
                            var jsonStr = firstRow["JsonResult"]?.ToString();
                            if (!string.IsNullOrEmpty(jsonStr))
                            {
                                try 
                                {
                                    var dashboard = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                                    if (dashboard != null)
                                    {
                                        foreach (var kvp in dashboard)
                                        {
                                            finalMessage = finalMessage.Replace("{" + kvp.Key + "}", kvp.Value?.ToString() ?? "0");
                                        }
                                    }
                                } catch { /* Fallback if not dictionary */ }
                            }
                        }
                        
                        // For standard row results, replace placeholders with formatted data
                        foreach (var kvp in formattedData)
                        {
                            finalMessage = finalMessage.Replace("{" + kvp.Key + "}", kvp.Value);
                        }
                    }

                    return Ok(new
                    {
                        query = commandResult.Sql,
                        data = data,
                        count = data.Count,
                        message = finalMessage,
                        totalTokens = commandResult.TotalTokens,
                        remainingCredits = commandResult.RemainingCredits
                    });
                }

                // 5. Default Response (Message or Fallback)
                return Ok(new
                {
                    query = "",
                    data = new List<object>(),
                    count = 0,
                    message = commandResult.SuccessMessage ?? commandResult.ClarificationMessage ?? commandResult.ErrorMessage ?? "I'm not sure how to help with that. Could you please rephrase or provide more details?",
                    totalTokens = commandResult.TotalTokens,
                    remainingCredits = commandResult.RemainingCredits
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}