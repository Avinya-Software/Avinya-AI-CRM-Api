using System.Security.Claims;
using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads
{
    public interface ILeadService
    {
        Task<ResponseModel> GetAllAsync(string tenantId);
        Task<ResponseModel> GetByIdAsync(Guid id, string tenantId);
        Task<ResponseModel> CreateAsync(LeadRequestDto dto, string userId);
        Task<ResponseModel> UpdateAsync(LeadRequestDto dto, string userId,string tenantId);
        Task<ResponseModel> UpdateLeadStatus(Guid id, Guid statusId);
        Task<ResponseModel> DeleteAsync(Guid id, string deletedBy, string tenantId);
        Task<ResponseModel> GetFilteredAsync(string? search, string? statusId, DateTime? startDate, DateTime? endDate, int page, int pageSize, string userId);
        Task<ResponseModel> GetAllSourceAsync();
        Task<ResponseModel> GetAllStatusAsync();
        Task<ResponseModel> GetLeadHistory(Guid leadId);

        Task<ResponseModel> GetAllLeadGrpByStatus();
    }
}
