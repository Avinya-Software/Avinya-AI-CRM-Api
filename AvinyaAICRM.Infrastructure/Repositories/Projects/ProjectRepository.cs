using AvinyaAICRM.Application.DTOs.Product;
using AvinyaAICRM.Application.DTOs.Projects;
using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Projects;
using AvinyaAICRM.Domain.Entities.Projects;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.Projects
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Project>> GetAllAsync(string tenantId)
        {
            return await _context.Projects
                .Where(x => x.TenantId == Guid.Parse(tenantId) && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(Guid id, string tenantId)
        {
            return await _context.Projects
                .FirstOrDefaultAsync(x =>
                    x.ProjectID == id &&
                    x.TenantId == Guid.Parse(tenantId) &&
                    !x.IsDeleted);
        }

        public async Task AddAsync(Project project)
        {
            try
            {
                await _context.Projects.AddAsync(project);
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {

            }
           
        }

        public async Task UpdateAsync(Project project)
        {
            _context.Projects.Update(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id, string tenantId)
        {
            var project = await GetByIdAsync(id, tenantId);
            if (project == null) return false;

            project.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<ProjectDto>> GetFilteredAsync(
    string? search,
    int? statusFilter,
    DateTime? startDate,
    DateTime? endDate,
    int pageNumber,
    int pageSize,
    string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            var query = _context.Projects
                .Where(p =>
                    !p.IsDeleted &&
                    p.TenantId == user.TenantId)
                .AsQueryable();

            #region SEARCH FILTER

            if (!string.IsNullOrWhiteSpace(search))
            {
                var like = $"%{search}%";

                query =
                    from p in query
                    join c in _context.Clients
                        on p.ClientID equals c.ClientID into pc
                    from client in pc.DefaultIfEmpty()
                    where
                        EF.Functions.Like(p.ProjectName, like) ||
                        EF.Functions.Like(client.CompanyName, like) ||
                        EF.Functions.Like(p.Location, like)
                    select p;
            }

            #endregion

            #region STATUS FILTER

            if (statusFilter.HasValue)
                query = query.Where(p => p.Status == statusFilter);

            #endregion

            #region DATE FILTER

            DateTime? fromDate = startDate?.Date;
            DateTime? toDate = endDate?.Date;

            if (fromDate.HasValue && !toDate.HasValue)
                toDate = DateTime.Today;

            if (toDate.HasValue)
                toDate = toDate.Value.AddDays(1).AddTicks(-1);

            if (fromDate.HasValue && toDate.HasValue)
                query = query.Where(p =>
                    p.CreatedDate >= fromDate &&
                    p.CreatedDate <= toDate);

            #endregion

            var totalRecords = await query.CountAsync();

            var projects = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectID).ToList();

            /* ===============================
               LOAD RELATED DATA
               =============================== */

            var clients = await _context.Clients
                .Where(c => projectIds.Contains(c.ClientID))
                .ToListAsync();

            var users = await _context.Users
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            /* ===============================
               ✅ LOAD TASKS FOR PROJECTS
               =============================== */

            var userGuid = Guid.Parse(userId);

            var taskOccurrences = await _context.TaskOccurrences
                .Include(x => x.TaskSeries)
                .Where(x =>
                    x.TaskSeries.ProjectId != null &&
                    projectIds.Contains(x.TaskSeries.ProjectId.Value) &&
                    (
                        x.TaskSeries.CreatedBy == userId
                        || x.AssignedTo == userId
                        || (
                            x.TaskSeries.TeamId != null &&
                            _context.TeamMembers.Any(tm =>
                                tm.TeamId == x.TaskSeries.TeamId &&
                                tm.UserId == userGuid)
                        )
                    ))
                .ToListAsync();

            var tasksGrouped = taskOccurrences
                .GroupBy(x => x.TaskSeries.ProjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new TaskDto
                    {
                        OccurrenceId = x.Id,
                        Title = x.TaskSeries.Title,
                        TeamId = x.TaskSeries.TeamId,
                        DueDateTime = x.DueDateTime,
                        Status = x.Status,
                        IsRecurring = x.TaskSeries.IsRecurring,
                        AssignedTo = x.AssignedTo
                    }).ToList()
                );

            /* ===============================
               DTO MAPPING
               =============================== */

            var result = projects.Select(p =>
            {
                var client = clients.FirstOrDefault(c => c.ClientID == p.ClientID);

                var managerName =
                    users.FirstOrDefault(u => u.Id == p.ProjectManagerId)?.UserName;

                var assignedUserName =
                    users.FirstOrDefault(u => u.Id == p.AssignedToUserId)?.UserName;

                return new ProjectDto
                {
                    ProjectID = p.ProjectID,
                    ProjectName = p.ProjectName,
                    Description = p.Description,

                    ClientID = p.ClientID,
                    ClientName = client?.CompanyName,

                    Location = p.Location,
                    Status = p.Status,
                    Priority = p.Priority,
                    ProgressPercent = p.ProgressPercent,

                    ProjectManagerId = p.ProjectManagerId,
                    ProjectManagerName = managerName,

                    AssignedToUserId = p.AssignedToUserId,
                    AssignedUserName = assignedUserName,

                    TeamId = p.TeamId,

                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Deadline = p.Deadline,

                    EstimatedValue = p.EstimatedValue,
                    Notes = p.Notes,
                    CreatedDate = p.CreatedDate,

                    // ✅ attach tasks
                    Tasks = tasksGrouped.ContainsKey(p.ProjectID)
                        ? tasksGrouped[p.ProjectID]
                        : new List<TaskDto>()
                };
            }).ToList();

            return new PagedResult<ProjectDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = result
            };
        }
    }
}
