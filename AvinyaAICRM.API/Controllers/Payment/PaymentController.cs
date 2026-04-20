using AvinyaAICRM.Application.DTOs.Payment;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AvinyaAICRM.API.Controllers.Payment
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? throw new UnauthorizedAccessException("Tenant ID missing.");
        private string GetUserId() => User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID missing.");

        [HttpGet("{paymentid}")]
        public async Task<IActionResult> GetById(Guid paymentid)
        {
            var result = await _paymentService.GetPaymentByIdAsync(paymentid);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("Getpaymentbyinvoice/{InvoiceID}")]
        public async Task<IActionResult> GetPaymentsByInvoiceIdAsync(Guid InvoiceID)
        {
            var result = await _paymentService.GetPaymentsByInvoiceIdAsync(InvoiceID);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            var result = await _paymentService.CreatePaymentAsync(dto, tenantId, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePaymentDto dto)
        {
            var tenantId = GetTenantId();
            var result = await _paymentService.UpdatePaymentAsync(dto, tenantId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
