using AvinyaAICRM.Application.DTOs.Projects;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Projects;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Projects;
using AvinyaAICRM.Domain.Entities.Projects;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.Projects
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectService(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<ResponseModel> GetAllAsync(string tenantId)
        {
            var data = await _projectRepository.GetAllAsync(tenantId);
            return CommonHelper.GetResponseMessage(data);
        }

        public async Task<ResponseModel> GetAllFilter(string? search,
 int? statusFilter,
 DateTime? startDate,
 DateTime? endDate,
 int pageNumber,
 int pageSize,
 string userId)
        {
            var data = await _projectRepository.GetFilteredAsync(search, statusFilter, startDate, endDate, pageNumber, pageSize, userId);
            return CommonHelper.GetResponseMessage(data);
        }

        public async Task<ResponseModel> GetByIdAsync(Guid id, string tenantId)
        {
            var project = await _projectRepository.GetByIdAsync(id, tenantId);
            if (project == null)
                return CommonHelper.BadRequestResponseMessage("Project not found");

            return CommonHelper.GetResponseMessage(project);
        }

        public async Task<ResponseModel> CreateAsync(ProjectCreateUpdateDto dto, string tenantId, string userId)
        {
            var project = new Project
            {
                ProjectID = Guid.NewGuid(),
                TenantId = Guid.Parse(tenantId),

                ProjectName = dto.ProjectName,
                Description = dto.Description,
                ClientID = dto.ClientID,
                Location = dto.Location,
                Status = dto.Status,
                Priority = dto.Priority,
                ProgressPercent = dto.ProgressPercent,

                ProjectManagerId = dto.ProjectManagerId,
                AssignedToUserId = dto.AssignedToUserId,
                TeamId = dto.TeamId,

                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Deadline = dto.Deadline,
                EstimatedValue = dto.EstimatedValue,
                Notes = dto.Notes,

                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            await _projectRepository.AddAsync(project);

            return CommonHelper.GetResponseMessage(project);
        }

        public async Task<ResponseModel> UpdateAsync(ProjectCreateUpdateDto dto, string tenantId)
        {
            var existing = await _projectRepository.GetByIdAsync(dto.ProjectID!.Value, tenantId);
            if (existing == null)
                return CommonHelper.BadRequestResponseMessage("Project not found");

            existing.ProjectName = dto.ProjectName;
            existing.Description = dto.Description;
            existing.Status = dto.Status;
            existing.Priority = dto.Priority;
            existing.ProgressPercent = dto.ProgressPercent;
            existing.ProjectManagerId = dto.ProjectManagerId;
            existing.AssignedToUserId = dto.AssignedToUserId;
            existing.TeamId = dto.TeamId;
            existing.UpdatedAt = DateTime.Now;

            await _projectRepository.UpdateAsync(existing);

            return CommonHelper.GetResponseMessage(existing);
        }

        public async Task<ResponseModel> DeleteAsync(Guid id, string tenantId)
        {
            var success = await _projectRepository.DeleteAsync(id, tenantId);
            if (!success)
                return CommonHelper.BadRequestResponseMessage("Project not found");

            return CommonHelper.SuccessResponseMessage("Project deleted successfully",null);
        }
    }
}
