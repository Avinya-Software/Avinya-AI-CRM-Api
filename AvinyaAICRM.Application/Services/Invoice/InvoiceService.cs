using AvinyaAICRM.Application.DTOs.Invoice;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Invoice;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Invoice;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.Invoice
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly INumberGeneratorService _numberGeneratorService;

        public InvoiceService(IInvoiceRepository invoiceRepository, INumberGeneratorService numberGeneratorService)
        {
            _invoiceRepository = invoiceRepository;
            _numberGeneratorService = numberGeneratorService;
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync(string tenantId)
        {
            var invoices = await _invoiceRepository.GetAllInvoicesAsync(tenantId);
            return invoices.Select(MapToDto);
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, string tenantId)
        {
            var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId, tenantId);
            return invoice != null ? MapToDto(invoice) : null;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string tenantId)
        {
            var invoice = new Domain.Entities.Invoice.Invoice
            {
                InvoiceID = Guid.NewGuid(),
                InvoiceNo = await _numberGeneratorService.GenerateNumberAsync("InvoiceNo", tenantId),
                OrderID = dto.OrderID,
                ClientID = dto.ClientID,
                InvoiceDate = dto.InvoiceDate,
                SubTotal = dto.SubTotal,
                Taxes = dto.Taxes,
                Discount = dto.Discount,
                GrandTotal = dto.GrandTotal,
                InvoiceStatusID = dto.InvoiceStatusID,
                CreatedDate = DateTime.Now,
                IsDeleted = false,
                PaidAmount = 0,
                PlaceOfSupply = dto.PlaceOfSupply,
                ReverseCharge = dto.ReverseCharge,
                GRRRNo = dto.GRRRNo,
                DueDate = dto.DueDate,
                Transport = dto.Transport,
                VehicleNo = dto.VehicleNo,
                Station = dto.Station,
                EWayBillNo = dto.EWayBillNo,
                OutstandingAmount = dto.GrandTotal - dto.Discount,
                RemainingPayment = (dto.GrandTotal - dto.Discount),
                TenantId = tenantId
            };

            var created = await _invoiceRepository.AddInvoiceAsync(invoice);

            return MapToDto(created);
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(UpdateInvoiceDto dto, string tenantId)
        {
            var existing = await _invoiceRepository.GetInvoiceByIdAsync(dto.InvoiceID, tenantId);
            if (existing == null) throw new Exception("Invoice not found or access denied.");

            existing.OrderID = dto.OrderID;
            existing.ClientID = dto.ClientID;
            existing.InvoiceDate = dto.InvoiceDate;
            existing.SubTotal = dto.SubTotal;
            existing.Taxes = dto.Taxes;
            existing.Discount = dto.Discount;
            
            existing.GrandTotal = dto.GrandTotal;
            existing.OutstandingAmount = existing.GrandTotal - existing.Discount;
            existing.RemainingPayment = existing.OutstandingAmount - existing.PaidAmount;
            existing.InvoiceStatusID = dto.InvoiceStatusID;
            existing.PlaceOfSupply = dto.PlaceOfSupply;
            existing.ReverseCharge = dto.ReverseCharge;
            existing.GRRRNo = dto.GRRRNo;
            existing.DueDate = dto.DueDate;
            existing.Transport = dto.Transport;
            existing.VehicleNo = dto.VehicleNo;
            existing.Station = dto.Station;
            existing.EWayBillNo = dto.EWayBillNo;

            var updated = await _invoiceRepository.UpdateInvoiceAsync(existing);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteInvoiceAsync(Guid invoiceId, string tenantId)
        {
            return await _invoiceRepository.DeleteInvoiceAsync(invoiceId, tenantId);
        }

        public async Task<AvinyaAICRM.Shared.Model.ResponseModel> GetFilteredAsync(
            string? search,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize,
            string userId)
        {
            var pagedResult = await _invoiceRepository.GetFilteredAsync(search, statusFilter, startDate, endDate, pageNumber, pageSize, userId);
            return new AvinyaAICRM.Shared.Model.ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Invoices retrieved successfully",
                Data = pagedResult
            };
        }

        public async Task<AvinyaAICRM.Shared.Model.ResponseModel> GetAllInvoiceStatusesAsync()
        {
            var statuses = await _invoiceRepository.GetAllInvoiceStatusesAsync();
            return new AvinyaAICRM.Shared.Model.ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Invoice statuses retrieved successfully",
                Data = statuses
            };
        }

        private InvoiceDto MapToDto(Domain.Entities.Invoice.Invoice invoice)
        {
            return new InvoiceDto
            {
                InvoiceID = invoice.InvoiceID,
                InvoiceNo = invoice.InvoiceNo,
                OrderID = invoice.OrderID,
                ClientID = invoice.ClientID,
                InvoiceDate = invoice.InvoiceDate,
                SubTotal = invoice.SubTotal,
                Taxes = invoice.Taxes,
                Discount = invoice.Discount,
                GrandTotal = invoice.GrandTotal,
                InvoiceStatusID = invoice.InvoiceStatusID,
                CreatedDate = invoice.CreatedDate,
                RemainingPayment = invoice.RemainingPayment,
                PaidAmount = invoice.PaidAmount,
                PlaceOfSupply = invoice.PlaceOfSupply,
                ReverseCharge = invoice.ReverseCharge,
                GRRRNo = invoice.GRRRNo,
                DueDate = invoice.DueDate,
                Transport = invoice.Transport,
                VehicleNo = invoice.VehicleNo,
                Station = invoice.Station,
                EWayBillNo = invoice.EWayBillNo,
                OutstandingAmount = invoice.OutstandingAmount
            };
        }
    }
}
