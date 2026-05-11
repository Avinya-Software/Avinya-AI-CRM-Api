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
        private readonly ICRMQueryService _crmService;
        private readonly ICreditService _credits;

        public AIController(ICRMQueryService crmService, ICreditService credits)
        {
            _crmService = crmService;
            _credits = credits;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromForm] AIRequest request)
        {
            if (!string.IsNullOrEmpty(request.Message) && request.Message.Length > 800)
                return BadRequest("Message is too long. Please restrict your question to 800 characters.");

            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var tenantIdClaim = User.FindFirst("tenantId")?.Value;
                var isSuperAdmin = User.IsInRole("SuperAdmin");

                if (string.IsNullOrEmpty(tenantIdClaim) && !isSuperAdmin)
                    return Unauthorized("User is not assigned to a valid tenant.");

                Guid tenantId = Guid.TryParse(tenantIdClaim, out var parsed) ? parsed : Guid.Empty;
                var allowedModules = await _crmService.GetUserAllowedModulesAsync(userId ?? "");

                var response = await _crmService.ProcessChatRequestAsync(request, tenantId, userId ?? "", isSuperAdmin, allowedModules);

                var mappedAction = MapAction(response.Action);
                var chatResponse = new ChatResponse
                {
                    Message          = response.Message,
                    Data             = response.Data ?? new List<Dictionary<string, object>>(),
                    Count            = response.Count,
                    Action           = mappedAction,
                    Parameters       = IsCreateAction(response.Action) ? response.Parameters : null,
                    Suggestions      = response.Suggestions,
                    CreditsUsed      = response.CreditsUsed,
                    RemainingCredits = response.RemainingCredits,
                    ErrorMessage     = string.IsNullOrEmpty(response.ErrorMessage) ? null : response.ErrorMessage
                };

                return Ok(chatResponse);
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

        private static string MapAction(string action) => action switch
        {
            "get_summary"    => "query",
            "create_lead"    => "create_lead",
            "create_task"    => "create_task",
            "create_expense" => "create_expense",
            _                => "message"
        };

        private static bool IsCreateAction(string action) =>
            action is "create_lead" or "create_task" or "create_expense";
    }
}
