using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadDetailsDto
    {
        public Guid LeadID { get; set; }
        public string? LeadNo { get; set; }
        public Guid? ClientID { get; set; }

        // Client Info
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }

        // Lead Info
        public DateTime? Date { get; set; }
        public string? RequirementDetails { get; set; }

        public string? LeadSourceID { get; set; }
        public string? LeadSourceName { get; set; }
        public string? OtherSources { get; set; }

        public string? Status { get; set; }
        public string? StatusName { get; set; }

        public string? Notes { get; set; }
        public string? Links { get; set; }

        // User Info
        public string? CreatedBy { get; set; }
        public string? CreatedbyName { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignToName { get; set; }

        public DateTime? CreatedDate { get; set; }

        // Client Extra
        public int ClientType { get; set; }
        public string? ClientTypeName { get; set; }
        public string? CompanyName { get; set; }
        public string? GSTNo { get; set; }
        public string? BillingAddress { get; set; }

        // ✅ Followup Summary
        public int FollowupCount { get; set; }
        public Guid? LatestLeadFollowupId { get; set; }
        public string? LatestFollowupStatus { get; set; }
        public DateTime? NextFollowupDate { get; set; }

        // ✅ Full Followup List
        public List<LeadFollowupDetailsDto>? Followups { get; set; }
    }
}
