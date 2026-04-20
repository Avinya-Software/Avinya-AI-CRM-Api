using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    // ══════════════════════════════════════════════════════════════════════════
    // SECTION A — COMBINED SUMMARY
    // ══════════════════════════════════════════════════════════════════════════

    public class TaskProjectSummaryKpiDto
    {
        // Project KPIs
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int OnHoldProjects { get; set; }
        public int AtRiskProjects { get; set; }     // past Deadline, not completed
        public double ProjectCompletionRate { get; set; }     // %
        public decimal TotalEstimatedValue { get; set; }     // sum of Projects.EstimatedValue
        public double AvgProjectProgress { get; set; }     // avg ProgressPercent

        // Task KPIs
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int SkippedTasks { get; set; }
        public int OverdueTasks { get; set; }     // past DueDateTime, not completed
        public int SlaBreachedTasks { get; set; }
        public double TaskCompletionRate { get; set; }     // %
        public double SlaBreachRate { get; set; }     // %
        public double AvgTaskCompletionHours { get; set; }     // StartDateTime → CompletedAt
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SECTION B — PROJECTS
    // ══════════════════════════════════════════════════════════════════════════

    public class ProjectStatusBreakdownDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public decimal TotalEstValue { get; set; }
    }

    public class ProjectPriorityBreakdownDto
    {
        public string PriorityName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int AtRisk { get; set; }
        public double Percentage { get; set; }
    }

    public class ProjectDetailRowDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ProjectManager { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string PriorityName { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? Deadline { get; set; }
        public int DaysRemaining { get; set; }   // negative = overdue
        public decimal? EstimatedValue { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public bool IsAtRisk { get; set; }   // deadline passed, not completed
    }

    public class ProjectAtRiskRowDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ProjectManager { get; set; } = string.Empty;
        public string PriorityName { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public DateTime Deadline { get; set; }
        public int DaysOverdue { get; set; }
        public int PendingTasks { get; set; }
        public int SlaBreachedTasks { get; set; }
    }

    public class ProjectTeamSummaryDto
    {
        public long TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public double AvgProgress { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SECTION C — TASKS
    // ══════════════════════════════════════════════════════════════════════════

    public class TaskStatusBreakdownDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class TaskScopeBreakdownDto
    {
        public string Scope { get; set; } = string.Empty;   // Personal | Team | Project
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Overdue { get; set; }
        public double CompletionRate { get; set; }
    }

    public class TaskUserWorkloadDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Overdue { get; set; }
        public int SlaBreached { get; set; }
        public double CompletionRate { get; set; }
        public double SlaBreachRate { get; set; }
        public double AvgCompletionHours { get; set; }
    }

    public class TaskOverdueRowDto
    {
        public long TaskOccurrenceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public DateTime DueDateTime { get; set; }
        public int HoursOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool SlaBreached { get; set; }
    }

    public class TaskSlaBreachRowDto
    {
        public long TaskOccurrenceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public DateTime SlaDeadline { get; set; }
        public DateTime BreachedAt { get; set; }
        public double BreachHours { get; set; }   // how many hours past SLA
        public string CurrentStatus { get; set; } = string.Empty;
    }

    public class TaskRecurringSeriesDto
    {
        public long SeriesId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RecurrenceRule { get; set; } = string.Empty;
        public int TotalOccurrences { get; set; }
        public int Completed { get; set; }
        public int Skipped { get; set; }
        public int Pending { get; set; }
        public double CompletionRate { get; set; }
        public int SlaBreaches { get; set; }
    }

    public class TaskMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalCreated { get; set; }
        public int Completed { get; set; }
        public int Overdue { get; set; }
        public int SlaBreached { get; set; }
        public double CompletionRate { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ROOT RESPONSE
    // ══════════════════════════════════════════════════════════════════════════

    public class TaskProjectReportDto
    {
        // A — Combined summary
        public TaskProjectSummaryKpiDto Summary { get; set; } = new();

        // B — Project sections
        public List<ProjectStatusBreakdownDto> ProjectStatusBreakdown { get; set; } = new();
        public List<ProjectPriorityBreakdownDto> ProjectPriorityBreakdown { get; set; } = new();
        public List<ProjectDetailRowDto> ProjectDetails { get; set; } = new();
        public List<ProjectAtRiskRowDto> ProjectsAtRisk { get; set; } = new();
        public List<ProjectTeamSummaryDto> ProjectTeamSummary { get; set; } = new();

        // C — Task sections
        public List<TaskStatusBreakdownDto> TaskStatusBreakdown { get; set; } = new();
        public List<TaskScopeBreakdownDto> TaskScopeBreakdown { get; set; } = new();
        public List<TaskUserWorkloadDto> TaskUserWorkload { get; set; } = new();
        public List<TaskOverdueRowDto> OverdueTasks { get; set; } = new();
        public List<TaskSlaBreachRowDto> SlaBreaches { get; set; } = new();
        public List<TaskRecurringSeriesDto> RecurringSeries { get; set; } = new();
        public List<TaskMonthlyTrendDto> TaskMonthlyTrend { get; set; } = new();
        public TaskProjectReportFilterDto AppliedFilters { get; set; } = new();
    }
}
