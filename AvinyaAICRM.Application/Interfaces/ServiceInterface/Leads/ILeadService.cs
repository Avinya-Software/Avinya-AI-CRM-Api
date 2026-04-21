using System.Security.Claims;
using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads
{
    public interface ILeadService
    {
        Task<ResponseModel> GetAllAsync(string tenantId, string? role);
        Task<ResponseModel> GetByIdAsync(Guid id, string tenantId, string? role);
        Task<ResponseModel> CreateAsync(LeadRequestDto dto, string userId);
        Task<ResponseModel> UpdateAsync(LeadRequestDto dto, string userId,string tenantId, string? role);
        Task<ResponseModel> UpdateLeadStatus(Guid id, Guid statusId);
        Task<ResponseModel> DeleteAsync(Guid id, string deletedBy, string tenantId, string? role);
        Task<ResponseModel> GetFilteredAsync(string? search, string? statusId, DateTime? startDate, DateTime? endDate, int page, int pageSize, string userId, string? role);
        Task<ResponseModel> GetAllSourceAsync();
        Task<ResponseModel> GetAllStatusAsync();
        Task<ResponseModel> GetLeadHistory(Guid leadId);

        Task<ResponseModel> GetAllLeadGrpByStatus();
    }
}
