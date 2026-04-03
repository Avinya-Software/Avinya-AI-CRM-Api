using AvinyaAICRM.Application.Interfaces.ServiceInterface.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvinyaAICRM.API.Controllers.Dashboard
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashbaordService _dashboardService;

        public DashboardController(IDashbaordService dashbaordService)
        {
            _dashboardService = dashbaordService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetDashbaord()
        {
            var userId = User.FindFirst("userId")?.Value!;
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var data = await _dashboardService.GetDashboardAsync(tenantId, role, userId);

            return new JsonResult(data) { StatusCode = data.StatusCode };
        }
    }
}
