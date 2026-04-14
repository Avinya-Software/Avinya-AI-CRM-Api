using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Report;
using AvinyaAICRM.Application.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvinyaAICRM.API.Controllers.Reports
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ILeadReportService _leadReportService;
        private readonly IClientReportService _clientReportService;
        private readonly IQuotationReportService _quotationReportService;
        private readonly IOrderReportService _orderReportService;
        private readonly IFinanceReportService _financeReportService;
        public ReportController(ILeadReportService leadReportService, IClientReportService clientReportService, IQuotationReportService quotationReportService, IOrderReportService orderReportService, IFinanceReportService financeReportService)
        {
            _leadReportService = leadReportService;
            _clientReportService = clientReportService;
            _quotationReportService = quotationReportService;
            _orderReportService = orderReportService;
            _financeReportService = financeReportService;
        }

        [HttpGet("lead-pipeline")]
        public async Task<IActionResult> GetLeadPipelineReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] Guid? leadSourceId,
            [FromQuery] Guid? leadStatusId,
            [FromQuery] string? assignedTo)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new LeadPipelineFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                LeadSourceId = leadSourceId,
                LeadStatusId = leadStatusId,
                AssignedTo = assignedTo,
                TenantId = tenantId
            };

            var result = await _leadReportService.GetLeadPipelineReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("lead-lifecycle")]
        public async Task<IActionResult> GetLeadLifecycleReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] Guid? leadSourceId,
            [FromQuery] Guid? leadStatusId,
            [FromQuery] string? assignedTo,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new LeadPipelineFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                LeadSourceId = leadSourceId,
                LeadStatusId = leadStatusId,
                AssignedTo = assignedTo,
                TenantId = tenantId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _leadReportService.GetLeadLifecycleReportAsync(filter);
            return Ok(result);
        }


        [HttpGet("client-revenue")]
        public async Task<IActionResult> GetClientRevenueReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? clientId,
        [FromQuery] int? clientType,
        [FromQuery] int? stateId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new ClientReportFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                ClientId = clientId,
                ClientType = clientType,
                StateId = stateId,
                TenantId = tenantId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _clientReportService.GetClientReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("client-drilldown/{clientId}")]
        public async Task<IActionResult> GetClientDrillDown(Guid clientId)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var result = await _clientReportService.GetClientDrillDownAsync(clientId, tenantId);
            return Ok(result);
        }

        [HttpGet("quotation")]
        public async Task<IActionResult> GetQuotationReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? quotationStatusId,
        [FromQuery] Guid? clientId,
        [FromQuery] string? createdBy,
        [FromQuery] int? firmId)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new QuotationReportFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                QuotationStatusId = quotationStatusId,
                ClientId = clientId,
                CreatedBy = createdBy,
                FirmId = firmId,
                TenantId = tenantId
            };

            var result = await _quotationReportService.GetQuotationReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("quotation-lifecycle")]
        public async Task<IActionResult> GetQuotationLifecycleReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? quotationStatusId,
        [FromQuery] Guid? clientId,
        [FromQuery] string? createdBy,
        [FromQuery] int? firmId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new QuotationReportFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                QuotationStatusId = quotationStatusId,
                ClientId = clientId,
                CreatedBy = createdBy,
                FirmId = firmId,
                TenantId = tenantId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _quotationReportService.GetQuotationLifecycleReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("order")]
        public async Task<IActionResult> GetOrderReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? orderStatusId,
        [FromQuery] int? designStatusId,
        [FromQuery] Guid? clientId,
        [FromQuery] string? assignedDesignTo,
        [FromQuery] int? firmId,
        [FromQuery] bool overdueOnly = false)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new OrderReportFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                OrderStatusId = orderStatusId,
                DesignStatusId = designStatusId,
                ClientId = clientId,
                AssignedDesignTo = assignedDesignTo,
                FirmId = firmId,
                OverdueOnly = overdueOnly,
                TenantId = tenantId
            };

            var result = await _orderReportService.GetOrderReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("order-lifecycle")]
        public async Task<IActionResult> GetOrderLifecycleReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? orderStatusId,
        [FromQuery] int? designStatusId,
        [FromQuery] Guid? clientId,
        [FromQuery] string? assignedDesignTo,
        [FromQuery] int? firmId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new OrderReportFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                OrderStatusId = orderStatusId,
                DesignStatusId = designStatusId,
                ClientId = clientId,
                AssignedDesignTo = assignedDesignTo,
                FirmId = firmId,
                TenantId = tenantId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _orderReportService.GetOrderLifecycleReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("finance")]
        public async Task<IActionResult> GetFinanceReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? invoiceStatusId,
        [FromQuery] Guid? clientId,
        [FromQuery] int? expenseCategoryId,
        [FromQuery] string? paymentMode,
        [FromQuery] bool overdueOnly = false)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized(new { StatusCode = 401, StatusMessage = "Invalid tenant." });

            var filter = new FinanceReportFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                InvoiceStatusId = invoiceStatusId,
                ClientId = clientId,
                ExpenseCategoryId = expenseCategoryId,
                PaymentMode = paymentMode,
                OverdueOnly = overdueOnly,
                TenantId = tenantId
            };

            var result = await _financeReportService.GetFinanceReportAsync(filter);
            return Ok(result);
        }
    }
}
