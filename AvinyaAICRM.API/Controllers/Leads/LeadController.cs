using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;

namespace AvinyaAICRM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeadController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadController(ILeadService leadService)
        {
            _leadService = leadService;
        }
        //[ApiExplorerSettings(IgnoreApi = true)]
        [SwaggerIgnore]
        [HttpGet("get-lead-dropdown-list")]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var response = await _leadService.GetAllAsync(tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var response = await _leadService.GetByIdAsync(id, tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPost]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> Create([FromBody] LeadRequestDto dto)
        {
            var userId = User.FindFirst("userId")?.Value!;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _leadService.CreateAsync(dto, userId);

            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPut("{id}")]   
        //[Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Update([FromBody] LeadRequestDto dto, Guid id)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var tenantId = User.FindFirst("tenantId")?.Value!;
            if (dto.LeadID == Guid.Empty)
                return BadRequest("LeadID is required");
            dto.LeadID = id;
            var response = await _leadService.UpdateAsync(dto, userId, tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPut("update-status")]
        //[Authorize(Roles = "Manager,Admin")]    
        public async Task<IActionResult> UpdateStatus([FromQuery] Guid id, [FromQuery] Guid statusId)
        {
            var response = await _leadService.UpdateLeadStatus(id, statusId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var tenantId = User.FindFirst("tenantId")?.Value!;
            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("Session expired. Please login again.");
            }
            var response = await _leadService.DeleteAsync(id, userId, tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("filter")]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetFiltered(
            string? search = null,
            string? status = null, DateTime? startDate = null, DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _leadService.GetFilteredAsync(search, status, startDate, endDate, page, pageSize, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("source-dropdown")]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetAllSource()
        {
            var response = await _leadService.GetAllSourceAsync();
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("status-dropdown")]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetAllStatus()
        {
            var response = await _leadService.GetAllStatusAsync();
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("{leadId}/history")]
        //[Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetLeadHistory(Guid leadId)
        {
            var response = await _leadService.GetLeadHistory(leadId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("GetAll-Lead-GrpStatus")]
        //[Authorize(Roles = "Employee,Manager,Admin")]

        public async Task<IActionResult> GetAllLeadGrpByStatus()
        {
            var response = await _leadService.GetAllLeadGrpByStatus();
            return new JsonResult(response) { StatusCode = response.StatusCode };

        }
    }
}
