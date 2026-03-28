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
        private readonly AppDbContext _context; 

        public AIController(IAIService aiService, ICRMQueryService crmService, AppDbContext context) 
        {
            _aiService = aiService;
            _crmService = crmService;
            _context = context; 
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

                // 1. Fetch User Permissions (Allowed Modules with 'view' action)
                var allowedModules = new List<string>();
                if (!string.IsNullOrEmpty(userId))
                {
                    allowedModules = await (from up in _context.UserPermissions
                                           join p in _context.Permissions on up.PermissionId equals p.PermissionId
                                           join mm in _context.Modules on p.ModuleId equals mm.ModuleId
                                           join a in _context.Actions on p.ActionId equals a.ActionId
                                           where up.UserId == userId && mm.IsActive == true && a.ActionKey.ToLower() == "view"
                                           select mm.ModuleKey.ToLower()).Distinct().ToListAsync();
                }

                // 2. Ask AI to generate SQL + Templates in ONE call
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

                // 3. Execute the query with ROLE/TENANT enforcement
                var data = await _crmService.ExecuteRawSqlAsync(aiResponse.Sql, tenantId, isSuperAdmin);

                // 4. Hydrate the template
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
