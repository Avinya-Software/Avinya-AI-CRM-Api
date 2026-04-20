using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Domain.Entities.Quotations;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations
{
    public interface IQuotationItemRepository
    {
        Task<IEnumerable<QuotationItemResponseDto>> GetAllAsync(Guid? quotationId = null);
        Task<QuotationItemResponseDto?> GetByIdAsync(Guid id);
        Task AddAsync(QuotationItem item);
        Task<QuotationItem?> UpdateAsync(QuotationItem item);
        Task<bool> DeleteAsync(Guid id);
        Task<PagedResult<QuotationItemResponseDto>> GetFilteredAsync(
     string? search,
     Guid? statusId,
     int pageNumber,
     int pageSize);
    }

}
