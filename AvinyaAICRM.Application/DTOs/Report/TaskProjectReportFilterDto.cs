using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class TaskProjectReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        /// <summary>Filter by ProjectStatusMaster.StatusID</summary>
        public int? ProjectStatusId { get; set; }

        /// <summary>Filter by ProjectPriorityMaster.PriorityID</summary>
        public int? PriorityId { get; set; }

        /// <summary>Filter tasks/projects by a specific client</summary>
        public Guid? ClientId { get; set; }

        /// <summary>Filter by project manager (AspNetUsers.Id)</summary>
        public string? ProjectManagerId { get; set; }

        /// <summary>Filter by team (Teams.Id)</summary>
        public long? TeamId { get; set; }

        /// <summary>Filter by task scope: Personal | Team | Project</summary>
        public string? TaskScope { get; set; }

        /// <summary>Filter by task assignee (AspNetUsers.Id)</summary>
        public string? AssignedTo { get; set; }

        /// <summary>true = show only overdue / SLA-breached records</summary>
        public bool AtRiskOnly { get; set; } = false;

        /// <summary>Drill into a specific project</summary>
        public Guid? ProjectId { get; set; }

        // Injected from JWT
        public Guid TenantId { get; set; }
    }
}
