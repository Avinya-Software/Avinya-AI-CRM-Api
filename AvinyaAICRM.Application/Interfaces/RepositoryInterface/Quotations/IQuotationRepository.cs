using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations
{
    public interface IQuotationRepository
    {
        Task<(QuotationResponseDto Quotation, bool IsNew)> PostOrPutAsync(QuotationRequestDto dto);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<QuotationResponseDto?> GetByIdAsync(Guid id);
        Task<PagedResult<QuotationResponseDto>> FilterAsync(
         string? search,
         string? statusFilter,
         DateTime? startDate,
         DateTime? endDate,
         int pageNumber,
         int pageSize);
        Task<IEnumerable<QuotationDropdown>> GetAllAsync();
    }
}
