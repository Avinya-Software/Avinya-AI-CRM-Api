

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class Followup
    {
        public Guid FollowUpID { get; set; }
        public Guid LeadID { get; set; }
        public string LeadName { get; set; }   
        public Guid? ClientID { get; set; }
        public string CompanyName { get; set; }
        public string ClientName { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? Notes { get; set; }
        public DateTime? NextFollowupDate { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? FollowUpBy { get; set; }
        public string FolloUpByName { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
