
namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadFollowupDto
    {
        public Guid FollowUpID { get; set; }
        public Guid LeadID { get; set; }
        public string LeadNo { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? Notes { get; set; }
        public DateTime? NextFollowupDate { get; set; }
        public int Status { get; set; }

        public string StatusName { get; set; }
        public string? FollowUpBy { get; set; }
        public string? FollowUpByName { get; set; }  
        public DateTime? CreatedDate { get; set; }
         public string? ClientName { get; set; }  
         public string? CompanyName { get; set; }  
         public string? Mobile { get; set; }  
         public string? Email { get; set; }  
    }
}
