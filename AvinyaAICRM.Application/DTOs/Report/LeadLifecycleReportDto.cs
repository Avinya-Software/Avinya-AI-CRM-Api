namespace AvinyaAICRM.Application.DTOs.Report
{
    public class LeadLifecycleReportDto
    {
        public Guid LeadID { get; set; }
        public string? LeadNo { get; set; }
        public DateTime? Date { get; set; }
        public string? RequirementDetails { get; set; }
        public string? ClientName { get; set; }
        public string? StatusName { get; set; }
        public string? SourceName { get; set; }
        public string? AssignedToName { get; set; }

        public List<LeadQuotationDto> Quotations { get; set; } = new();
        public List<LeadOrderDto> Orders { get; set; } = new();
        public List<LeadFollowupDto> Followups { get; set; } = new();
    }

    public class LeadQuotationDto
    {
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public decimal GrandTotal { get; set; }
        public string? StatusName { get; set; }
    }

    public class LeadOrderDto
    {
        public Guid OrderID { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal GrandTotal { get; set; }
        public string? StatusName { get; set; }
    }

    public class LeadFollowupDto
    {
        public Guid FollowUpID { get; set; }
        public string? Notes { get; set; }
        public DateTime? NextFollowupDate { get; set; }
        public string? StatusName { get; set; }
        public string? FollowUpByName { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
