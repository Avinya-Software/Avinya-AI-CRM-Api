using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Tenant
{
    public class Tenant
    {
        public Guid TenantId { get; set; }
        public string CompanyName { get; set; }
        public string? IndustryType { get; set; }
        public string CompanyEmail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? Address { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBySuperAdminId { get; set; }
    }

}
