using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Domain.Entities.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers
{
    [ApiController]
    [Route("api/FollowUp")]
    [Authorize]
    public class LeadFollowupController : ControllerBase
    {
        private readonly ILeadFollowupService _service;

        public LeadFollowupController(ILeadFollowupService service)
        {
            _service = service;
        }

        [HttpGet("get-leadfollowpstatus-dropdown-list")]
        public async Task<IActionResult> GetAllLeadFollowupStatuses()
        {
            var result = await _service.GetAllLeadFollowupStatusesAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {      
            var result = await _service.GetAllAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { statusCode = 404, statusMessage = "Follow-up not found" });

            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("add")]
        public async Task<IActionResult> Create([FromBody] LeadFollowups dto)
        {
            var result = await _service.CreateAsync(dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] LeadFollowups dto)
        {
            dto.FollowUpID = id;
            var result = await _service.UpdateAsync(dto);

            if (result == null)
                return NotFound(new { statusCode = 404, statusMessage = "Follow-up not found" });

            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _service.DeleteAsync(id);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered(string? search = null, string? status = null, Guid? LeadId = null, int page = 1, int pageSize = 10)
        {
            var response = await _service.GetFilteredAsync(search, status, LeadId, page, pageSize);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }
       
        [HttpGet("lead/{leadId}")]
        public async Task<IActionResult> GetLeadFollowupHistory(Guid leadId)
        {
            var result = await _service.GetFollowupHistoryAsync(leadId);

            if (result.StatusCode == 404)
                return NotFound(result);

            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
