using AvinyaAICRM.Domain.Entities.Invoice;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Invoice
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<Domain.Entities.Invoice.Invoice>> GetAllInvoicesAsync(string tenantId);
        Task<Domain.Entities.Invoice.Invoice?> GetInvoiceByIdAsync(Guid invoiceId, string tenantId);
        Task<Domain.Entities.Invoice.Invoice> AddInvoiceAsync(Domain.Entities.Invoice.Invoice invoice);
        Task<Domain.Entities.Invoice.Invoice> UpdateInvoiceAsync(Domain.Entities.Invoice.Invoice invoice);
        Task<bool> DeleteInvoiceAsync(Guid invoiceId, string tenantId);
        Task<AvinyaAICRM.Shared.Model.PagedResult<AvinyaAICRM.Application.DTOs.Invoice.InvoiceDto>> GetFilteredAsync(
            string? search,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize,
            string userId);
        Task<IEnumerable<InvoiceStatus>> GetAllInvoiceStatusesAsync();
    }
}
