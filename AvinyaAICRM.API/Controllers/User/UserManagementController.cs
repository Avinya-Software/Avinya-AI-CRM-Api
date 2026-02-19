using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.User
{
    [ApiController]
    [Route("api/users")]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _service;

        public UserManagementController(IUserManagementService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateUser(CreateUserRequestModel request)
        {
            var userId = User.FindFirst("userId")?.Value;
            var result = await _service.CreateUserAsync(request, userId!);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPut("update")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUser(UpdateUserRequestModel request)
        {
            var userId = User.FindFirst("userId")?.Value;
            var result = await _service.UpdateUserAsync(request, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("assign-permissions")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AssignPermissions(AssignPermissionsRequestModel request)
        {
            var userId = User.FindFirst("userId")?.Value;
            var result = await _service.AssignPermissionsAsync(request, userId!);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpGet("me/menu")]
        public async Task<IActionResult> GetMenu()
        {
            var userId = User.FindFirst("userId")?.Value;
            var result = await _service.GetMenuAsync(userId!);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpGet("companies")]
        public async Task<IActionResult> GetMyCompanies()
        {
            var result = await _service.GetMyCompaniesAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpGet("users-dropdown")]
        public async Task<IActionResult> GetUsersDropdown()
        {
            var userId = User.FindFirst("userId")?.Value;
            var result = await _service.GetUsersDropdown(userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
