using AvinyaAICRM.Application.Interfaces.ServiceInterface.Permission;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Permission
{
    [ApiController]
    [Route("api/permission")]
    public class PermissionController : ControllerBase
    {
        private readonly IUserManagementService _service;
        private readonly IPermissionService _permissionService;
        public PermissionController(IUserManagementService service, IPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        
        [HttpGet("me/permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            var userId = User.FindFirst("userId")?.Value;
            var result = await _service.GetMyPermissionsAsync(userId!);
            return new JsonResult(result) { StatusCode = result.StatusCode };

        }

        [HttpGet("list")]
        public async Task<IActionResult> GetPermissionList()
        {
            var result = await _permissionService.GetPermissionListAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }


    }
}
