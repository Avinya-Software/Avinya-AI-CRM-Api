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

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("users")]
        public async Task<IActionResult> GetUsers([FromBody] UserListFilterRequest request)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            Guid? currentUserTenant = null;

            if (!isSuperAdmin)
            {
                var tenantIdClaim = User.FindFirst("tenantId")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var parsed))
                {
                    currentUserTenant = parsed;
                }
            }

            var result = await _userService.GetUsersForSuperAdminAsync(request, currentUserTenant);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("users-list")]
        public async Task<IActionResult> GetUsersList([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? role, [FromQuery] Guid? tenantId,
            [FromQuery] bool? isActive, [FromQuery] string? search, [FromQuery] string? fullName, [FromQuery] string? email)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            Guid? currentUserTenant = null;

            if (!isSuperAdmin)
            {
                var tenantIdClaim = User.FindFirst("tenantId")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var parsed))
                {
                    currentUserTenant = parsed;
                }
            }

            var request = new UserListFilterRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                FullName = fullName,
                Email = email,
                Role = role,
                TenantId = tenantId, // This comes from query, but repository should handle priority
                IsActive = isActive,
            };
            var result = await _userService.GetUsersForSuperAdminAsync(request, currentUserTenant);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

    }
}
