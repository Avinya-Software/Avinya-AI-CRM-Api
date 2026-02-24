

namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadHistoryDto
    {
        public string EntityType { get; set; }
        public string Action { get; set; }
        public string ClientName { get; set; }

        public string CompanyName { get; set; }
        public DateTime? Createddate { get; set; }
        public string? Status { get; set; }

        public string StatusName { get; set; }
    }
}
