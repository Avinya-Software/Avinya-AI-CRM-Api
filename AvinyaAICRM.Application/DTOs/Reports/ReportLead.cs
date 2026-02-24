

namespace VaraPrints.Application.DTOs.ReportsDTO
{
    public class ReportLead
    {
        public Guid LeadID { get; set; }
        public Guid? ClientID { get; set; }
        public string? CompanyName { get; set; }

        public string? ClientName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public DateTime? Date { get; set; }
        public string? RequirementDetails { get; set; }

        public string? LeadSourceID { get; set; }
        public string? LeadSourceName { get; set; }

        public string? Status { get; set; }
        public string? StatusName { get; set; }
        public string? FollowUpStatusName { get; set; }

        public DateTime? NextFollowupDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignToName { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int QuotationsCount { get; set; }
        public int FollowupsCount { get; set; }
        public int OrdersCount { get; set; }
    }
}
