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
        private readonly IQuotationPdfService _pdfService;

        public QuotationController(IQuotationService quotationService, IQuotationPdfService pdfService)
        {
            _quotationService = quotationService;
            _pdfService = pdfService;
        }

        [HttpGet("download-pdf/{id}")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var result = await _quotationService.GetByIdAsync(id);
            if (result.StatusCode != 200 || result.Data == null)
            {
                return NotFound("Quotation not found.");
            }

            var quotation = (QuotationResponseDto)result.Data;
            var pdfBytes = _pdfService.GenerateQuotationPdf(quotation);

            return File(pdfBytes, "application/pdf", $"Quotation_{quotation.QuotationNo}.pdf");
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
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _quotationService.AddOrUpdateAsync(dto, userId);
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
            var userId = User.FindFirst("userId")?.Value!;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var result = await _quotationService.FilterAsync(search, status, startDate, endDate, page, pageSize, userId, role);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}