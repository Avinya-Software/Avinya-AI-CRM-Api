namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class UpcomingFollowupDto
    {
        public Guid LeadID { get; set; }
        public string LeadNo { get; set; }
        public DateTime? NextFollowupDate { get; set; }
    }
}
