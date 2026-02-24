namespace AvinyaAICRM.Application.DTOs.Quotation
{
   
    public class QuotationResponseDto
    {
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; }
        public Guid? ClientID { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string BillAddress { get; set; }
        public string ShippingAddress { get; set; }
        public string GstNo { get; set; }
        public string ClientName { get; set; }
        public Guid? LeadID { get; set; }
        public string LeadNo { get; set; }
        public bool EnableTax { get; set; }
        public DateTime QuotationDate { get; set; }
        public DateTime ValidTill { get; set; }
        public Guid Status { get; set; }
        public string StatusName { get; set; }
        public string TermsAndConditions { get; set; }
        public string RejectedNotes { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Taxes { get; set; }
        public decimal GrandTotal { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public int FirmID { get; set; }
        public string FirmName { get; set; }
        public string? FirmGSTNo { get; set; }
        public string? FirmAddress { get; set; }
        public string FirmMobile { get; set; }
        public List<QuotationItemResponseDto> Items { get; set; }
    }

}
