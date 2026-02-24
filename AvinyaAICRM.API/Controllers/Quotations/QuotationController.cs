using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace AvinyaAICRM.Api.Controllers.Quotations
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _quotationService;

        public QuotationController(IQuotationService quotationService)
        {
            _quotationService = quotationService;
        }

        [HttpGet("get-quotation-dropdown-list")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _quotationService.GetAllAsync();
            return new JsonResult(response) { StatusCode = response.StatusCode };

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _quotationService.GetByIdAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("addOrUpdate")]
        public async Task<IActionResult> AddOrUpdate([FromBody] QuotationRequestDto dto)
        {
            var result = await _quotationService.AddOrUpdateAsync(dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _quotationService.SoftDeleteAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            DateTime? startDate = null, 
            DateTime? endDate = null,
            [FromQuery] int pageSize = 10)
        {
            var result = await _quotationService.FilterAsync(search, status, startDate, endDate, page, pageSize);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}