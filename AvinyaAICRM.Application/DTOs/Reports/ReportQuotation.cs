namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportQuotation
    {
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; }
        public int TotalQuotationItems { get; set; }
        public string ProductName { get; set; }
        public Guid? ClientID { get; set; }
        public string CompanyName { get; set; }
        public string ClientName { get; set; }
        public Guid? LeadID { get; set; }
        public string LeadName { get; set; }
        public DateTime QuotationDate { get; set; }
        public DateTime ValidTill { get; set; }
        public Guid Status { get; set; }
        public string StatusName { get; set; }
        public string RejectedNotes { get; set; }
        public string TermsAndConditions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Taxes { get; set; }
        public decimal GrandTotal { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
