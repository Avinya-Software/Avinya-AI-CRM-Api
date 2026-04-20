namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class RecentQuotationDto
    {
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; }
        public string ClientName { get; set; }
        public decimal GrandTotal { get; set; }
    }
}
