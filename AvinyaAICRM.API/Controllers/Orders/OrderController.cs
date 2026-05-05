using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvinyaAICRM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;
        private readonly IStatusDropDownServices _statusDropDownServices;
        private readonly IOrderPdfService _pdfService;
        private readonly IDocumentEmailService _documentEmailService;

        public OrderController(IOrderService service, IStatusDropDownServices statusDropDownServices, IOrderPdfService pdfService, IDocumentEmailService documentEmailService)
        {
            _service = service;
            _statusDropDownServices = statusDropDownServices;
            _pdfService = pdfService;
            _documentEmailService = documentEmailService;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var result = await _service.GetByIdAsync(id, tenantId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(string? search, int page = 1, int pageSize = 10, int? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var result = await _service.GetFilteredAsync(search, page, pageSize, userId, role, status, startDate, endDate);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] OrderDto dto)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _service.AddOrUpdateAsync(dto, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("get-OrderStatus-dropdown")]
        public async Task<IActionResult> GetOrderStatusDropDown()
        {
            var result = await _statusDropDownServices.GetAllOrderStatusAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("get-DesignStatus-dropdown")]
        public async Task<IActionResult> GetDesignStatusDropDown()
        {
            var result = await _statusDropDownServices.GetAllDesignStatusAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("download-pdf/{id}")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var result = await _service.GetByIdAsync(id, tenantId);
            if (result.StatusCode != 200 || result.Data == null)
            {
                return NotFound("Order not found.");
            }

            var order = (OrderResponseDto)result.Data;
            var pdfBytes = _pdfService.GenerateOrderPdf(order);

            if (pdfBytes == null)
            {
                return NotFound("Failed to generate PDF.");
            }

            return File(pdfBytes, "application/pdf", $"Order_{order.OrderNo ?? "Order"}.pdf");
        }

        [HttpPost("send-email/{id}")]
        public async Task<IActionResult> SendEmail(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var result = await _service.GetByIdAsync(id, tenantId);
            if (result.StatusCode != 200 || result.Data == null)
            {
                return NotFound("Order not found.");
            }

            var order = (OrderResponseDto)result.Data;
            if (string.IsNullOrEmpty(order.Email))
            {
                return BadRequest("Client email is missing.");
            }

            var pdfBytes = _pdfService.GenerateOrderPdf(order);
            if (pdfBytes == null)
            {
                return BadRequest("Failed to generate PDF.");
            }

            await _documentEmailService.SendOrderEmailAsync(order.Email, order.ClientName ?? "", order.OrderNo ?? "", pdfBytes);

            return Ok(new { message = "Email sent successfully" });
        }
    }
}
