using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Shared.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using AvinyaAICRM.Infrastructure.Persistence; 

namespace AvinyaAICRM.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ICRMQueryService _crmService;

        public AIController(IAIService aiService, ICRMQueryService crmService) 
        {
            _aiService = aiService;
            _crmService = crmService;
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

                // 2. Analyze Intent / Process Command
                var commandResult = await _crmService.ProcessCommandAsync(request.Message, tenantId, userId ?? "", isSuperAdmin);

                if (commandResult.Action == "create_lead")
                {
                    return Ok(new
                    {
                        action = commandResult.Action,
                        message = commandResult.ClarificationMessage,
                        isClarificationRequired = commandResult.IsClarificationRequired,
                        suggestedClients = commandResult.SuggestedClients,
                        data = new List<object>(),
                        count = 0
                    });
                }

                // 3. Fallback to SQL Generation (Queries)
                var aiResponse = await _aiService.GenerateSqlAsync(request.Message, tenantId, isSuperAdmin, allowedModules);

                if (string.IsNullOrEmpty(aiResponse?.Sql))
                {
                    return Ok(new
                    {
                        query = "",
                        data = new List<object>(),
                        count = 0,
                        message = aiResponse?.ErrorMessage ?? "I couldn't understand the request or generate a valid query."
                    });
                }

                // 4. Execute the query
                var data = await _crmService.ExecuteRawSqlAsync(aiResponse.Sql, tenantId, isSuperAdmin);

                // 5. Hydrate the template
                var finalMessage = data.Count > 0 
                    ? aiResponse.SuccessMessage.Replace("{count}", data.Count.ToString())
                    : aiResponse.ErrorMessage;

                return Ok(new
                {
                    query = aiResponse.Sql, 
                    data = data,
                    count = data.Count,
                    message = finalMessage
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
