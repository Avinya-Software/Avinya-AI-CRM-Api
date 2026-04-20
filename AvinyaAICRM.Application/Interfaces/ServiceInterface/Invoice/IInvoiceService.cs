using AvinyaAICRM.Application.DTOs.Invoice;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Invoice
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync(string tenantId);
        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, string tenantId);
        Task<InvoiceDto?> GetInvoiceWithIteamByIdAsync(Guid invoiceId, string tenantId);
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string tenantId);
        Task<InvoiceDto> UpdateInvoiceAsync(UpdateInvoiceDto dto, string tenantId);
        Task<ResponseModel> GetFilteredAsync(
            string? search,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize,
            string userId);
        Task<ResponseModel> GetAllInvoiceStatusesAsync();
    }
}
