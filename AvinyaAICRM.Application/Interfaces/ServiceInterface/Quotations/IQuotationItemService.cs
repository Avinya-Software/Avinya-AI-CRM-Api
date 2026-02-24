using AvinyaAICRM.Shared.Model;
using AvinyaAICRM.Domain.Entities.Quotations;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations
{
    public interface IQuotationItemService
    {
        Task<ResponseModel> GetAllAsync(Guid? quotationId = null);
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(QuotationItem dto);
        Task<ResponseModel> UpdateAsync(Guid id, QuotationItem dto);
        Task<ResponseModel> DeleteAsync(Guid id);
        Task<ResponseModel> GetFilteredAsync(string? search, Guid? statusId, int page, int pageSize);
    }
}
