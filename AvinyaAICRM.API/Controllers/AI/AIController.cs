using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Shared.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;

namespace AvinyaAICRM.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ICRMQueryService _crmService;
        private readonly IAIKnowledgeService _knowledge;
        private readonly ICreditService _credits;

        public AIController(
            IAIService aiService, 
            ICRMQueryService crmService,
            IAIKnowledgeService knowledge,
            ICreditService credits)
        {
            _aiService = aiService;
            _crmService = crmService;
            _knowledge = knowledge;
            _credits = credits;
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> Feedback([FromBody] AvinyaAICRM.Application.DTOs.AI.AIFeedbackDto feedback)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var tenantIdClaim = User.FindFirst("tenantId")?.Value;
                Guid tenantId = Guid.TryParse(tenantIdClaim, out var parsed) ? parsed : Guid.Empty;

                if (!string.IsNullOrEmpty(feedback.UserCorrection) && feedback.UserCorrection.Length > 800)
                {
                    return BadRequest("Feedback correction is too long. Please restrict to 800 characters.");
                }

                string finalSql = feedback.GeneratedSql;

                List<Dictionary<string, object>> data = null;
                string successMessage = "I've corrected the query based on your feedback.";

                int usedTokens = 0;

                // If it's a correction, we "Heal" the query first
                if (!feedback.IsGood && !string.IsNullOrWhiteSpace(feedback.UserCorrection))
                {
                    var aiResult = await _aiService.RefineQueryAsync(feedback.OriginalMessage, feedback.GeneratedSql, feedback.UserCorrection, tenantId);
                    finalSql = aiResult.Sql ?? feedback.GeneratedSql;
                    usedTokens = aiResult.TotalTokens;

                    // Deduct Credits
                    if (usedTokens > 0)
                    {
                        await _credits.DeductCreditsAsync(userId ?? "", usedTokens, "AI_FEEDBACK_CORRECTION");
                    }
                    
                    // Re-run the query
                    try {
                        data = await _crmService.ExecuteRawSqlAsync(finalSql, tenantId, User.IsInRole("SuperAdmin"));
                    } catch (Exception ex) {
                        successMessage = "I refined the query, but encountered an error running it: " + ex.Message;
                    }
                }

                await _knowledge.SaveFeedbackAsync(feedback.OriginalMessage, finalSql, feedback.IsGood, userId, feedback.UserCorrection);
                
                var balance = await _credits.GetRemainingCreditsAsync(userId ?? "");
                
                return Ok(new { 
                    success = true, 
                    sql = finalSql, 
                    data = data,
                    message = successMessage,
                    count = data?.Count ?? 0,
                    remainingCredits = balance,
                    totalTokens = usedTokens
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving feedback: {ex.Message}");
            }
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIRequest request)
        {
            if (!string.IsNullOrEmpty(request.Message) && request.Message.Length > 800)
            {
                return BadRequest("Message is too long. Please restrict your question to 800 characters.");
            }
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
                var allowedModules = await _crmService.GetUserAllowedModulesAsync(userId ?? "");

                // Unified processing logic in service
                var response = await _crmService.ProcessChatRequestAsync(request, tenantId, userId ?? "", isSuperAdmin, allowedModules);

                return Ok(response);
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