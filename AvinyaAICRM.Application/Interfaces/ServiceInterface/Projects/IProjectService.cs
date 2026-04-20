using AvinyaAICRM.Application.DTOs.Projects;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Projects
{
    public interface IProjectService
    {
        Task<ResponseModel> GetAllAsync(string tenantId);
        Task<ResponseModel> GetByIdAsync(Guid id, string tenantId);
        Task<ResponseModel> CreateAsync(ProjectCreateUpdateDto dto, string tenantId, string userId);
        Task<ResponseModel> UpdateAsync(ProjectCreateUpdateDto dto, string tenantId);
        Task<ResponseModel> DeleteAsync(Guid id, string tenantId);
        Task<ResponseModel> GetAllFilter(string? search,
         int? statusFilter,
         DateTime? startDate,
         DateTime? endDate,
         int pageNumber,
         int pageSize,
         string userId);
    }
}
