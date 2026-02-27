using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace AvinyaAICRM.Domain.Entities.Quotations
{
    [Table("Quotations")]
    public class Quotation
    {
        [Key]
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; }
        public Guid? ClientID { get; set; }
        public Guid? LeadID { get; set; }
        public DateTime QuotationDate { get; set; }
        public DateTime ValidTill { get; set; }
        public Guid Status { get; set; }
        public int FirmID { get; set; }
        public string? RejectedNotes { get; set; }
        public string? TermsAndConditions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Taxes { get; set; }
        public decimal GrandTotal { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public bool IsDeleted { get; set; }
        public bool EnableTax { get; set; }
        public Guid? TenantId { get; set; }
    }

}
