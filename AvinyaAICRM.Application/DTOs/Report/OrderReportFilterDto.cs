using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class OrderReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        /// <summary>Filter by OrderStatusMaster.StatusID</summary>
        public int? OrderStatusId { get; set; }

        /// <summary>Filter by DesignStatusMaster.DesignStatusID</summary>
        public int? DesignStatusId { get; set; }

        /// <summary>Filter by a specific client</summary>
        public Guid? ClientId { get; set; }

        /// <summary>Filter by assigned designer (AspNetUsers.Id)</summary>
        public string? AssignedDesignTo { get; set; }

        /// <summary>Filter by FirmID</summary>
        public int? FirmId { get; set; }

        /// <summary>true = only overdue orders (past ExpectedDeliveryDate)</summary>
        public bool OverdueOnly { get; set; } = false;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Injected from JWT
        public Guid TenantId { get; set; }
    }
}
