using AvinyaAICRM.Application.DTOs.Projects;
using AvinyaAICRM.Domain.Entities.Projects;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Projects
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetAllAsync(string tenantId);
        Task<Project?> GetByIdAsync(Guid id, string tenantId);
        Task AddAsync(Project project);
        Task UpdateAsync(Project project);
        Task<bool> DeleteAsync(Guid id, string tenantId);
        Task<PagedResult<ProjectDto>> GetFilteredAsync(
    string? search,
    int? statusFilter,
    DateTime? startDate,
    DateTime? endDate,
    int pageNumber,
    int pageSize,
    string userId);
    }
}
