using AvinyaAICRM.Application.DTOs.Invoice;
using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Invoice;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AvinyaAICRM.API.Controllers.Invoice
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IOrderService _orderService;
        private readonly IInvoicePdfService _pdfService;

        public InvoiceController(IInvoiceService invoiceService, IOrderService orderService, IInvoicePdfService pdfService)
        {
            _invoiceService = invoiceService;
            _orderService = orderService;
            _pdfService = pdfService;
        }

        private string GetTenantId()
        {
            return User.FindFirst("tenantId")?.Value ?? throw new UnauthorizedAccessException("Tenant ID is missing.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenantId = GetTenantId();
            var result = await _invoiceService.GetAllInvoicesAsync(tenantId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = GetTenantId();
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, tenantId);
            if (invoice == null) return NotFound("Invoice not found.");
            return Ok(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
        {
            var tenantId = GetTenantId();
            var result = await _invoiceService.CreateInvoiceAsync(dto, tenantId);
            return CreatedAtAction(nameof(GetById), new { id = result.InvoiceID }, result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateInvoiceDto dto)
        {
            var tenantId = GetTenantId();
            try
            {
                var result = await _invoiceService.UpdateInvoiceAsync(dto, tenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = GetTenantId();
            var success = await _invoiceService.DeleteInvoiceAsync(id, tenantId);
            if (!success) return NotFound("Invoice not found.");
            return NoContent();
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered(
            string? search = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _invoiceService.GetFilteredAsync(search, status, startDate, endDate, page, pageSize, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("status-dropdown")]
        public async Task<IActionResult> GetAllStatus()
        {
            var response = await _invoiceService.GetAllInvoiceStatusesAsync();
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("download-pdf/{id}")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var tenantId = GetTenantId();
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, tenantId);
            if (invoice == null) return NotFound("Invoice not found.");

            // Standard Invoice uses Order details for printing
            if (!Guid.TryParse(invoice.OrderID, out Guid orderId))
            {
                return BadRequest("Invalid Order ID associated with this invoice.");
            }

            var result = await _orderService.GetByIdAsync(orderId, tenantId);
            if (result.StatusCode != 200 || result.Data == null)
            {
                return NotFound("Associated order details not found.");
            }

            var order = (OrderResponseDto)result.Data;
            
            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, order);

            if (pdfBytes == null)
            {
                return NotFound("Failed to generate PDF.");
            }

            return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceNo}.pdf");
        }
    }
}
