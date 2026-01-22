using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ISuperAdminService _superAdminService;

        public SuperAdminController(ISuperAdminService superAdminService)
        {
            _superAdminService = superAdminService;
        }

        [HttpPost("approve-admin")]
        public async Task<IActionResult> ApproveAdmin([FromQuery] Guid tenantId)
        {
            var result = await _superAdminService.ApproveAdminAsync(tenantId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
