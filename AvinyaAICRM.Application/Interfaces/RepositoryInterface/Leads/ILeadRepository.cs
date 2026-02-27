using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Shared.Model;
using System.Security.Claims;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads
{
    public interface ILeadRepository
    {
        Task<IEnumerable<LeadDropdown>> GetAllAsync(string tenantId);
        Task<LeadDto?> GetByIdAsync(Guid id, string tenantId);
        Task<Lead> AddAsync(LeadRequestDto dto, string userId);
        Task<Lead?> UpdateAsync(LeadRequestDto dto, string tenantId);
        Task<ResponseModel> UpdateLeadStatusAsync(Lead lead);
        Task<Lead?> GetLeadByIdAsync(Guid Id);
        Task<bool> DeleteAsync(Guid id, string deletedBy);

        Task<PagedResult<LeadDto>> GetFilteredAsync(
     string? search,
     string? statusFilter,
     DateTime? startDate,
     DateTime? endDate,
     int pageNumber,
     int pageSize,
     string userId);
        Task<IEnumerable<LeadSourceMaster>> GetAllLeadSourceAsync();
        Task<IEnumerable<LeadStatusMaster>> GetAllLeadStatusAsync();
        Task<DateTime?> GetLatestFollowupDateAsync(Guid leadId);

        Task<List<LeadHistoryDto>> GetLeadHistoryAsync(Guid leadId);

        Task<List<LeadGroupDto>> GetAllLeadGrpByStatus();

        // to check validation
        Task<(bool IsValid, string? Message)> ValidateClientAsync(LeadRequestDto dto);

    }

}