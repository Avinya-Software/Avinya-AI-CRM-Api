using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.ReportRepository
{
    public class TaskProjectReportRepository : ITaskProjectReportRepository
    {
        private readonly AppDbContext _context;

        public TaskProjectReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskProjectReportDto> GetTaskProjectReportAsync(TaskProjectReportFilterDto filter)
        {
            var today = DateTime.Now;

            // ── Master lookups ────────────────────────────────────────────────────
            var projectStatusMap = await _context.ProjectStatusMaster
                .ToDictionaryAsync(s => s.StatusID, s => s.StatusName);

            var projectPriorityMap = await _context.ProjectPriorityMaster
                .ToDictionaryAsync(p => p.PriorityID, p => p.PriorityName);

            var clientMap = await _context.Clients
                .Where(c => !c.IsDeleted && c.TenantId == filter.TenantId && c.IsCustomer)
                .ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);

            var userMap = await _context.Users
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var teamMap = await _context.Teams
                .Where(t => t.TenantId == filter.TenantId && t.IsActive)
                .Select(t => new { t.Id, t.Name, t.ManagerId })
                .ToDictionaryAsync(t => t.Id);

            // ════════════════════════════════════════════════════════════════════
            // SECTION B — PROJECTS
            // ════════════════════════════════════════════════════════════════════

            var projectQuery = _context.Projects
                .Where(p => p.IsDeleted != true && p.TenantId == filter.TenantId);


            if (filter.DateTo.HasValue)
                projectQuery = projectQuery.Where(p => p.CreatedDate <= filter.DateTo.Value);

            if (filter.ProjectStatusId.HasValue)
                projectQuery = projectQuery.Where(p => p.Status == filter.ProjectStatusId.Value);
            if (filter.PriorityId.HasValue)
                projectQuery = projectQuery.Where(p => p.PriorityID == filter.PriorityId.Value);
            if (filter.ClientId.HasValue)
                projectQuery = projectQuery.Where(p => p.ClientID == filter.ClientId.Value);
            if (!string.IsNullOrEmpty(filter.ProjectManagerId))
                projectQuery = projectQuery.Where(p => p.ProjectManagerId == filter.ProjectManagerId);
            if (filter.TeamId.HasValue)
                projectQuery = projectQuery.Where(p => p.TeamId == filter.TeamId.Value);
            if (filter.ProjectId.HasValue)
                projectQuery = projectQuery.Where(p => p.ProjectID == filter.ProjectId.Value);

            var projects = await projectQuery
                .Select(p => new
                {
                    p.ProjectID,
                    p.ProjectName,
                    p.ClientID,
                    p.Status,
                    p.PriorityID,
                    p.ProgressPercent,
                    p.ProjectManagerId,
                    p.AssignedToUserId,
                    p.TeamId,
                    p.StartDate,
                    p.EndDate,
                    p.Deadline,
                    p.EstimatedValue,
                    p.CreatedDate,
                    p.Notes
                })
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectID).ToList();

            // ── Helper: project status name
            string GetProjectStatus(int statusId) =>
                projectStatusMap.TryGetValue(statusId, out var s) ? s : string.Empty;

            bool IsProjectCompleted(int statusId) =>
                GetProjectStatus(statusId).Contains("Complet", StringComparison.OrdinalIgnoreCase);

            bool IsProjectAtRisk(DateTime? deadline, int statusId) =>
                deadline.HasValue &&
                deadline.Value < today &&
                !IsProjectCompleted(statusId);

            // ── Project KPIs
            int totalProjects = projects.Count;
            int activeProjects = projects.Count(p => !IsProjectCompleted(p.Status) &&
                                                         GetProjectStatus(p.Status) != "On Hold");
            int completedProjects = projects.Count(p => IsProjectCompleted(p.Status));
            int onHoldProjects = projects.Count(p =>
                GetProjectStatus(p.Status).Contains("Hold", StringComparison.OrdinalIgnoreCase));
            int atRiskProjects = projects.Count(p => IsProjectAtRisk(p.Deadline, p.Status));

            double avgProgress = projects.Any()
                ? Math.Round(projects.Average(p => p.ProgressPercent ?? 0), 1) : 0;

            // ── Status breakdown
            var projectStatusBreakdown = projectStatusMap.Select(sm =>
            {
                var group = projects.Where(p => p.Status == sm.Key).ToList();
                return new ProjectStatusBreakdownDto
                {
                    StatusName = sm.Value,
                    Count = group.Count,
                    Percentage = totalProjects > 0
                        ? Math.Round((double)group.Count / totalProjects * 100, 1) : 0,
                    TotalEstValue = group.Sum(p => p.EstimatedValue ?? 0)
                };
            })
            .OrderByDescending(s => s.Count)
            .ToList();

            // ── Priority breakdown
            var projectPriorityBreakdown = projectPriorityMap.Select(pm =>
            {
                var group = projects.Where(p => p.PriorityID == pm.Key).ToList();
                int atRisk = group.Count(p => IsProjectAtRisk(p.Deadline, p.Status));
                return new ProjectPriorityBreakdownDto
                {
                    PriorityName = pm.Value,
                    Count = group.Count,
                    AtRisk = atRisk,
                    Percentage = totalProjects > 0
                        ? Math.Round((double)group.Count / totalProjects * 100, 1) : 0
                };
            })
            .OrderByDescending(p => p.Count)
            .ToList();

            // ── Team summary
            var projectTeamSummary = projects
                .Where(p => p.TeamId.HasValue)
                .GroupBy(p => p.TeamId!.Value)
                .Select(g =>
                {
                    var list = g.ToList();
                    var team = teamMap.TryGetValue(g.Key, out var t) ? t : null;
                    int completed = list.Count(p => IsProjectCompleted(p.Status));
                    int active = list.Count(p => !IsProjectCompleted(p.Status) &&
                                                     GetProjectStatus(p.Status) != "On Hold");
                    double avgProg = list.Any()
                        ? Math.Round(list.Average(p => p.ProgressPercent ?? 0), 1) : 0;

                    return new ProjectTeamSummaryDto
                    {
                        TeamId = g.Key,
                        TeamName = team?.Name ?? "—",
                        ManagerName = team != null && userMap.ContainsKey(team.ManagerId.ToString())
                                               ? userMap[team.ManagerId.ToString()] : "—",
                        TotalProjects = list.Count,
                        ActiveProjects = active,
                        CompletedProjects = completed,
                        AvgProgress = avgProg
                    };
                })
                .OrderByDescending(t => t.TotalProjects)
                .ToList();

            // ════════════════════════════════════════════════════════════════════
            // SECTION C — TASKS
            // ════════════════════════════════════════════════════════════════════

            // Load TaskSeries scoped to this tenant
            // TaskSeries doesn't have TenantId — scope via TeamId (tenant's teams) or ProjectId
            var tenantTeamIds = teamMap.Keys.Select(k => (long?)k).ToList();
            var tenantProjectIds = projectIds.Select(p => (Guid?)p).ToList();

            var seriesQuery = _context.TaskSeries
                .Where(ts => ts.IsActive &&
                             (tenantTeamIds.Contains(ts.TeamId) ||
                              tenantProjectIds.Contains(ts.ProjectId)));

            if (!string.IsNullOrEmpty(filter.TaskScope))
                seriesQuery = seriesQuery.Where(ts => ts.TaskScope == filter.TaskScope);
            if (filter.ProjectId.HasValue)
                seriesQuery = seriesQuery.Where(ts => ts.ProjectId == filter.ProjectId.Value);
            if (filter.TeamId.HasValue)
                seriesQuery = seriesQuery.Where(ts => ts.TeamId == filter.TeamId.Value);

            var taskSeries = await seriesQuery
                .Select(ts => new
                {
                    ts.Id,
                    ts.Title,
                    ts.Description,
                    ts.IsRecurring,
                    ts.RecurrenceRule,
                    ts.StartDate,
                    ts.EndDate,
                    ts.CreatedBy,
                    ts.TeamId,
                    ts.TaskScope,
                    ts.Priority,
                    ts.ProjectId,
                    ts.CreatedAt
                })
                .ToListAsync();

            var seriesIds = taskSeries.Select(ts => ts.Id).ToList();

            // ── Task occurrences in date range
            var occurrenceQuery = _context.TaskOccurrences
                .Where(o => seriesIds.Contains(o.TaskSeriesId));

            if (filter.DateFrom.HasValue)
                occurrenceQuery = occurrenceQuery.Where(o => o.CreatedAt >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                occurrenceQuery = occurrenceQuery.Where(o => o.CreatedAt <= filter.DateTo.Value);
            if (!string.IsNullOrEmpty(filter.AssignedTo))
                occurrenceQuery = occurrenceQuery.Where(o => o.AssignedTo == filter.AssignedTo);
            if (filter.AtRiskOnly)
                occurrenceQuery = occurrenceQuery.Where(o =>
                    o.DueDateTime.HasValue && o.DueDateTime.Value < today &&
                    o.Status != "Completed" && o.Status != "Skipped");

            var occurrences = await occurrenceQuery
                .Select(o => new
                {
                    o.Id,
                    o.TaskSeriesId,
                    o.DueDateTime,
                    o.StartDateTime,
                    o.EndDateTime,
                    o.Status,
                    o.CompletedAt,
                    o.SkippedReason,
                    o.AssignedTo,
                    o.CreatedAt
                })
                .ToListAsync();

            var occurrenceIds = occurrences.Select(o => o.Id).ToList();


            // ── Helpers for tasks
            bool IsTaskOverdue(DateTime? due, string status) =>
                due.HasValue && due.Value < today &&
                status != "Completed" && status != "Skipped";


            // ── Task KPIs
            int totalTasks = occurrences.Count;
            int pendingTasks = occurrences.Count(o => o.Status == "Pending");
            int inProgressTasks = occurrences.Count(o => o.Status == "In Progress");
            int completedTasks = occurrences.Count(o => o.Status == "Completed");
            int skippedTasks = occurrences.Count(o => o.Status == "Skipped");
            int overdueTasks = occurrences.Count(o => IsTaskOverdue(o.DueDateTime, o.Status));

            // Avg completion hours: StartDateTime → CompletedAt
            double avgCompletionHours = 0;
            var completedWithTimes = occurrences
                .Where(o => o.Status == "Completed" &&
                            o.StartDateTime.HasValue &&
                            o.CompletedAt.HasValue)
                .ToList();
            if (completedWithTimes.Any())
            {
                avgCompletionHours = Math.Round(
                    completedWithTimes.Average(o =>
                        (o.CompletedAt!.Value - o.StartDateTime!.Value).TotalHours), 1);
            }

            // ── Status breakdown
            var allStatuses = new[] { "Pending", "In Progress", "Completed", "Skipped" };
            var taskStatusBreakdown = allStatuses.Select(s => new TaskStatusBreakdownDto
            {
                Status = s,
                Count = occurrences.Count(o => o.Status == s),
                Percentage = totalTasks > 0
                    ? Math.Round((double)occurrences.Count(o => o.Status == s) / totalTasks * 100, 1) : 0
            }).ToList();

            // ── Scope breakdown
            var allScopes = new[] { "Personal", "Team", "Project" };
            var taskScopeBreakdown = allScopes.Select(scope =>
            {
                var scopeSeriesIds = taskSeries
                    .Where(ts => ts.TaskScope == scope)
                    .Select(ts => ts.Id)
                    .ToHashSet();

                var scopeOcc = occurrences.Where(o => scopeSeriesIds.Contains(o.TaskSeriesId)).ToList();
                int sCompleted = scopeOcc.Count(o => o.Status == "Completed");
                int sOverdue = scopeOcc.Count(o => IsTaskOverdue(o.DueDateTime, o.Status));

                return new TaskScopeBreakdownDto
                {
                    Scope = scope,
                    Total = scopeOcc.Count,
                    Completed = sCompleted,
                    Overdue = sOverdue,
                    CompletionRate = scopeOcc.Any()
                        ? Math.Round((double)sCompleted / scopeOcc.Count * 100, 1) : 0
                };
            }).ToList();

            // ── User workload
            var taskUserWorkload = occurrences
                .Where(o => !string.IsNullOrEmpty(o.AssignedTo))
                .GroupBy(o => o.AssignedTo!)
                .Select(g =>
                {
                    var list = g.ToList();
                    int uCompleted = list.Count(o => o.Status == "Completed");
                    int uInProgress = list.Count(o => o.Status == "In Progress");
                    int uOverdue = list.Count(o => IsTaskOverdue(o.DueDateTime, o.Status));
       

                    // Avg completion hours for this user
                    var userCompleted = list
                        .Where(o => o.Status == "Completed" &&
                                    o.StartDateTime.HasValue && o.CompletedAt.HasValue)
                        .ToList();
                    double uAvgHours = userCompleted.Any()
                        ? Math.Round(userCompleted.Average(o =>
                            (o.CompletedAt!.Value - o.StartDateTime!.Value).TotalHours), 1) : 0;

                    return new TaskUserWorkloadDto
                    {
                        UserId = g.Key,
                        FullName = userMap.TryGetValue(g.Key, out var fn) ? fn : g.Key,
                        TotalAssigned = list.Count,
                        Completed = uCompleted,
                        InProgress = uInProgress,
                        Overdue = uOverdue,
                        CompletionRate = list.Any()
                            ? Math.Round((double)uCompleted / list.Count * 100, 1) : 0,
                        AvgCompletionHours = uAvgHours
                    };
                })
                .OrderByDescending(u => u.TotalAssigned)
                .ToList();

            // ── Overdue tasks list
            var seriesMap = taskSeries.ToDictionary(ts => ts.Id);
            var projectNameMap = projects.ToDictionary(p => p.ProjectID, p => p.ProjectName);

            var overdueTaskList = occurrences
                .Where(o => IsTaskOverdue(o.DueDateTime, o.Status))
                .Select(o =>
                {
                    var series = seriesMap.TryGetValue(o.TaskSeriesId, out var s) ? s : null;
                    var projectName = series?.ProjectId.HasValue == true &&
                                     projectNameMap.ContainsKey(series.ProjectId!.Value)
                                         ? projectNameMap[series.ProjectId!.Value] : string.Empty;

                    return new TaskOverdueRowDto
                    {
                        TaskOccurrenceId = o.Id,
                        Title = series?.Title ?? "—",
                        AssignedTo = !string.IsNullOrEmpty(o.AssignedTo) &&
                                           userMap.ContainsKey(o.AssignedTo)
                                               ? userMap[o.AssignedTo] : o.AssignedTo ?? "—",
                        Scope = series?.TaskScope ?? "—",
                        Priority = series?.Priority ?? "—",
                        ProjectName = projectName,
                        DueDateTime = o.DueDateTime!.Value,
                        HoursOverdue = (int)(today - o.DueDateTime!.Value).TotalHours,
                        Status = o.Status,
                    };
                })
                .OrderByDescending(t => t.HoursOverdue)
                .ToList();

            // ── SLA breach detail list
            var teamNameMap = teamMap.ToDictionary(k => k.Key, v => v.Value.Name);

            // ── Recurring series health
            var recurringSeries = taskSeries
                .Where(ts => ts.IsRecurring)
                .Select(ts =>
                {
                    var tsOcc = occurrences.Where(o => o.TaskSeriesId == ts.Id).ToList();
                    int tsCompleted = tsOcc.Count(o => o.Status == "Completed");
                    int tsSkipped = tsOcc.Count(o => o.Status == "Skipped");
                    int tsPending = tsOcc.Count(o => o.Status == "Pending");

                    return new TaskRecurringSeriesDto
                    {
                        SeriesId = ts.Id,
                        Title = ts.Title,
                        RecurrenceRule = ts.RecurrenceRule ?? string.Empty,
                        TotalOccurrences = tsOcc.Count,
                        Completed = tsCompleted,
                        Skipped = tsSkipped,
                        Pending = tsPending,
                        CompletionRate = tsOcc.Any()
                            ? Math.Round((double)tsCompleted / tsOcc.Count * 100, 1) : 0,
                    };
                })
                .OrderByDescending(ts => ts.SlaBreaches)
                .ToList();

            // ── Monthly trend for tasks
            var taskMonthlyTrend = occurrences
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var list = g.ToList();
                    int mComp = list.Count(o => o.Status == "Completed");
                    int mOverdue = list.Count(o => IsTaskOverdue(o.DueDateTime, o.Status));

                    return new TaskMonthlyTrendDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        TotalCreated = list.Count,
                        Completed = mComp,
                        Overdue = mOverdue,
                        CompletionRate = list.Any()
                            ? Math.Round((double)mComp / list.Count * 100, 1) : 0
                    };
                })
                .ToList();

            // ── Project detail rows (with task counts per project)
            var projectDetails = projects.Select(p =>
            {
                var projSeriesIds = taskSeries
                    .Where(ts => ts.ProjectId == p.ProjectID)
                    .Select(ts => ts.Id)
                    .ToHashSet();

                var projOcc = occurrences.Where(o => projSeriesIds.Contains(o.TaskSeriesId)).ToList();
                int projCompleted = projOcc.Count(o => o.Status == "Completed");
                int projOverdue = projOcc.Count(o => IsTaskOverdue(o.DueDateTime, o.Status));

                var deadlineDate = p.Deadline ?? null;
                int daysRemaining = deadlineDate.HasValue
                    ? (int)(deadlineDate.Value - today).TotalDays : 0;

                return new ProjectDetailRowDto
                {
                    ProjectId = p.ProjectID,
                    ProjectName = p.ProjectName,
                    CompanyName = p.ClientID.HasValue && clientMap.ContainsKey(p.ClientID.Value)
                                          ? clientMap[p.ClientID.Value] : "—",
                    ProjectManager = !string.IsNullOrEmpty(p.ProjectManagerId) &&
                                      userMap.ContainsKey(p.ProjectManagerId)
                                          ? userMap[p.ProjectManagerId] : "—",
                    TeamName = p.TeamId.HasValue && teamMap.ContainsKey(p.TeamId.Value)
                                          ? teamMap[p.TeamId.Value].Name : "—",
                    StatusName = GetProjectStatus(p.Status),
                    PriorityName = p.PriorityID.HasValue &&
                                      projectPriorityMap.ContainsKey(p.PriorityID.Value)
                                          ? projectPriorityMap[p.PriorityID.Value] : "—",
                    ProgressPercent = p.ProgressPercent ?? 0,
                    StartDate = p.StartDate ?? null,
                    Deadline = deadlineDate,
                    DaysRemaining = daysRemaining,
                    EstimatedValue = p.EstimatedValue,
                    TotalTasks = projOcc.Count,
                    CompletedTasks = projCompleted,
                    OverdueTasks = projOverdue,
                    IsAtRisk = IsProjectAtRisk(p.Deadline, p.Status)
                };
            })
            .OrderByDescending(p => p.IsAtRisk)
            .ThenBy(p => p.DaysRemaining)
            .ToList();

            // ── Projects at risk list
            var projectsAtRisk = projects
                .Where(p => IsProjectAtRisk(p.Deadline, p.Status))
                .Select(p =>
                {
                    var projSeriesIds = taskSeries
                        .Where(ts => ts.ProjectId == p.ProjectID)
                        .Select(ts => ts.Id)
                        .ToHashSet();

                    var projOcc = occurrences.Where(o => projSeriesIds.Contains(o.TaskSeriesId)).ToList();
                    int pendingCount = projOcc.Count(o => o.Status is "Pending" or "In Progress");
                    var deadlineDate = p.Deadline!.Value.Date;

                    return new ProjectAtRiskRowDto
                    {
                        ProjectId = p.ProjectID,
                        ProjectName = p.ProjectName,
                        CompanyName = p.ClientID.HasValue && clientMap.ContainsKey(p.ClientID.Value)
                                               ? clientMap[p.ClientID.Value] : "—",
                        ProjectManager = !string.IsNullOrEmpty(p.ProjectManagerId) &&
                                           userMap.ContainsKey(p.ProjectManagerId)
                                               ? userMap[p.ProjectManagerId] : "—",
                        PriorityName = p.PriorityID.HasValue &&
                                           projectPriorityMap.ContainsKey(p.PriorityID.Value)
                                               ? projectPriorityMap[p.PriorityID.Value] : "—",
                        ProgressPercent = p.ProgressPercent ?? 0,
                        Deadline = deadlineDate,
                        DaysOverdue = (int)(today - deadlineDate).TotalDays,
                        PendingTasks = pendingCount,
                    };
                })
                .OrderByDescending(p => p.DaysOverdue)
                .ToList();

            // ── Summary KPI assembly
            var summary = new TaskProjectSummaryKpiDto
            {
                TotalProjects = totalProjects,
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                OnHoldProjects = onHoldProjects,
                AtRiskProjects = atRiskProjects,
                ProjectCompletionRate = totalProjects > 0
                    ? Math.Round((double)completedProjects / totalProjects * 100, 1) : 0,
                TotalEstimatedValue = projects.Sum(p => p.EstimatedValue ?? 0),
                AvgProjectProgress = avgProgress,
                TotalTasks = totalTasks,
                PendingTasks = pendingTasks,
                InProgressTasks = inProgressTasks,
                CompletedTasks = completedTasks,
                SkippedTasks = skippedTasks,
                OverdueTasks = overdueTasks,
                TaskCompletionRate = totalTasks > 0
                    ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0,
                AvgTaskCompletionHours = avgCompletionHours
            };

            return new TaskProjectReportDto
            {
                Summary = summary,
                ProjectStatusBreakdown = projectStatusBreakdown,
                ProjectPriorityBreakdown = projectPriorityBreakdown,
                ProjectDetails = projectDetails,
                ProjectsAtRisk = projectsAtRisk,
                ProjectTeamSummary = projectTeamSummary,
                TaskStatusBreakdown = taskStatusBreakdown,
                TaskScopeBreakdown = taskScopeBreakdown,
                TaskUserWorkload = taskUserWorkload,
                OverdueTasks = overdueTaskList,
                RecurringSeries = recurringSeries,
                TaskMonthlyTrend = taskMonthlyTrend,
                AppliedFilters = filter
            };
        }
    }
}
