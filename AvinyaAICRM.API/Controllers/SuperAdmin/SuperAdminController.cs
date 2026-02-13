using AvinyaAICRM.Application.DTOs.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ISuperAdminService _superAdminService;
        private readonly IUserManagementService _userService;

        public SuperAdminController(ISuperAdminService superAdminService, IUserManagementService userService)
        {
            _superAdminService = superAdminService;
            _userService = userService;
        }

        [HttpPost("approve-admin")]
        public async Task<IActionResult> ApproveAdmin([FromQuery] Guid tenantId)
        {
            var result = await _superAdminService.ApproveAdminAsync(tenantId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("users")]
        public async Task<IActionResult> GetUsers([FromBody] UserListFilterRequest request)
        {
            var result = await _userService.GetUsersForSuperAdminAsync(request);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("users-list")]
        public async Task<IActionResult> GetUsersList([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? role, [FromQuery] Guid? tenantId,
            [FromQuery] bool? isActive, [FromQuery] string? search)
        {
            var request = new UserListFilterRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                Role = role,
                TenantId = tenantId,
                IsActive = isActive,
            };
            var result = await _userService.GetUsersForSuperAdminAsync(request);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

    }
}
