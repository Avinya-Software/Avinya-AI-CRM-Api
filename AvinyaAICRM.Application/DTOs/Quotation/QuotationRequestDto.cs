namespace AvinyaAICRM.Application.DTOs.Quotation
{

    public class QuotationRequestDto
    {
        public Guid? QuotationID { get; set; }
        public Guid? ClientID { get; set; }
        public Guid? LeadID { get; set; }
        public DateTime QuotationDate { get; set; }
        public DateTime? ValidTill { get; set; }
        public Guid? Status { get; set; }
        public int FirmID { get; set; }
        public bool? EnableTax { get; set; }
        public string? RejectedNotes { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? CreatedBy { get; set; }
        public List<QuotationItemDto> Items { get; set; } = new();
    }


}
