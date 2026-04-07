using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Tenants
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ITenantService _tenantService;
        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }
       
        [HttpGet("get")]
        public async Task<IActionResult> GetByIdAsync(Guid TenantId)
        {
            var result = await _tenantService.GetByIdAsync(TenantId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("Update")]
        public async Task<IActionResult> UpdateAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant)
        {
            var result = await _tenantService.UpdateAsync(tenant);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
