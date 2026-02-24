using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads
{
    public interface ILeadFollowupRepository
    {
        Task<IEnumerable<LeadFollowupStatus>> GetLeadFollowupStatusAsync();
        Task<IEnumerable<LeadFollowupDto>> GetAllAsync();
        Task<LeadFollowupDto?> GetByIdAsync(Guid id);
        Task<(LeadFollowups? Data, string? Error)> AddAsync(LeadFollowups dto);
        Task<LeadFollowups?> UpdateAsync(LeadFollowups dto);
        Task<bool> DeleteAsync(Guid id);
        Task<PagedResult<LeadFollowupDto>> GetFilteredAsync(
     string? search,
     string? status,
     Guid? leadId,
     int pageNumber,
     int pageSize);
        Task<(bool leadExists, List<LeadFollowupDto>? followups)> GetFollowupHistoryAsync(Guid leadId);
    }
}
