using AvinyaAICRM.Domain.Entities.Invoice;
using AvinyaAICRM.Shared.Model;
using AvinyaAICRM.Application.DTOs.Invoice;


namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Invoices
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync(string tenantId);
        Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, string tenantId);
        Task<InvoiceDto> GetInvoiceWithIteamByIdAsync(Guid invoiceId, string tenantId);
        Task<Invoice> AddInvoiceAsync(Invoice invoice);
        Task<Invoice> UpdateInvoiceAsync(Invoice invoice);

        Task<PagedResult<InvoiceDto>> GetFilteredAsync(
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
