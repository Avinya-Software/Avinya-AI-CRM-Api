using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads
{
    public interface ILeadFollowupService
    {
        Task<ResponseModel> GetAllAsync();
        Task<ResponseModel> GetAllLeadFollowupStatusesAsync();
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(LeadFollowups dto);
        Task<ResponseModel> UpdateAsync(LeadFollowups dto);
        Task<ResponseModel> DeleteAsync(Guid id);
        Task<ResponseModel> GetFilteredAsync(string? search, string? status, Guid? LeadId, int page, int pageSize);

        Task<ResponseModel> GetFollowupHistoryAsync(Guid leadId);
    }
}
