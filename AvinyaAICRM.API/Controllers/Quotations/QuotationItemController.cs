using AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations;
using AvinyaAICRM.Domain.Entities.Quotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuotationItemController : ControllerBase
    {
        private readonly IQuotationItemService _service;

        public QuotationItemController(IQuotationItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? quotationId)
        {
            var result = await _service.GetAllAsync(quotationId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuotationItem dto)
        {
            var result = await _service.CreateAsync(dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] QuotationItem dto)
        {
            dto.QuotationItemID = id;
            var result = await _service.UpdateAsync(id, dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered([FromQuery] string? search, [FromQuery] Guid? statusId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetFilteredAsync(search, statusId, page, pageSize);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
