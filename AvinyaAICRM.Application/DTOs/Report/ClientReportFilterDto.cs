using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class ClientReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        /// <summary>Drill into a single client</summary>
        public Guid? ClientId { get; set; }

        /// <summary>ClientType int from Clients table</summary>
        public int? ClientType { get; set; }

        /// <summary>Filter by state</summary>
        public int? StateId { get; set; }

        // Injected from JWT
        public Guid TenantId { get; set; }
    }
}
