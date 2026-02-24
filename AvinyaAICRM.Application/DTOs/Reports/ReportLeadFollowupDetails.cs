

namespace VaraPrints.Application.DTOs.ReportsDTO
{
    public class ReportLeadFollowupDetails
    {
        public FollowupInfo Followup { get; set; }
        public LeadInfo Lead { get; set; }
        public Client Client { get; set; }
    }

    public class FollowupInfo
    {
        public string? Notes { get; set; }
        public DateTime? NextFollowupDate { get; set; }
        public string? StatusName { get; set; }
        public string? FollowUpByName { get; set; }
    }

    public class LeadInfo
    {
        public Guid LeadID { get; set; }
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? LeadSourceName { get; set; }
        public string? StatusName { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Client
    {
        public Guid? ClientID { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? GSTNo { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public string? BillingAddress { get; set; }
    }
}
