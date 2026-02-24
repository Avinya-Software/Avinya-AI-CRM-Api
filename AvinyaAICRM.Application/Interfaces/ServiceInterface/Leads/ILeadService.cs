using System.Security.Claims;
using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads
{
    public interface ILeadService
    {
        Task<ResponseModel> GetAllAsync();
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(LeadRequestDto dto, string userId);
        Task<ResponseModel> UpdateAsync(LeadRequestDto dto);
        Task<ResponseModel> UpdateLeadStatus(Guid id, Guid statusId);
        Task<ResponseModel> DeleteAsync(Guid id, string deletedBy);
        Task<ResponseModel> GetFilteredAsync(string? search, string? statusId, DateTime? startDate, DateTime? endDate, int page, int pageSize, ClaimsPrincipal user);
        Task<ResponseModel> GetAllSourceAsync();
        Task<ResponseModel> GetAllStatusAsync();
        Task<ResponseModel> GetLeadHistory(Guid leadId);

        Task<ResponseModel> GetAllLeadGrpByStatus();
    }
}
