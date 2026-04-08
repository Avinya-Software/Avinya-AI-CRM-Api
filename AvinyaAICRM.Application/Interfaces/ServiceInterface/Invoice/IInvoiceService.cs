using AvinyaAICRM.Application.DTOs.Invoice;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Invoice
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync(string tenantId);
        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, string tenantId);
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string tenantId);
        Task<InvoiceDto> UpdateInvoiceAsync(UpdateInvoiceDto dto, string tenantId);
        Task<bool> DeleteInvoiceAsync(Guid invoiceId, string tenantId);
        Task<AvinyaAICRM.Shared.Model.ResponseModel> GetFilteredAsync(
            string? search,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize,
            string userId);
        Task<AvinyaAICRM.Shared.Model.ResponseModel> GetAllInvoiceStatusesAsync();
    }
}
