using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class QuotationReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        /// <summary>Filter by QuotationStatusMaster.QuotationStatusID</summary>
        public Guid? QuotationStatusId { get; set; }

        /// <summary>Filter by a specific client</summary>
        public Guid? ClientId { get; set; }

        /// <summary>Filter by creator user</summary>
        public string? CreatedBy { get; set; }

        /// <summary>Filter by FirmID if multi-firm setup</summary>
        public int? FirmId { get; set; }

        // Injected from JWT
        public Guid TenantId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
