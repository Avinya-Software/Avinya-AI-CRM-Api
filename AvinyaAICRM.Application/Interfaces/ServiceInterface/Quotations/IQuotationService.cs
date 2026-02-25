using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations
{
    public interface IQuotationService
    {
        Task<ResponseModel> GetAllAsync();
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> AddOrUpdateAsync(QuotationRequestDto dto, string userId);
        Task<ResponseModel> SoftDeleteAsync(Guid id);
        Task<ResponseModel> FilterAsync(string? search,string? statusFilter, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    }
}
