using AvinyaAICRM.Domain.Entities.Quotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AvinyaAICRM.Domain.Entities.Orders
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public Guid OrderID { get; set; }

        [Required]
        public string OrderNo { get; set; } = null!;

        [Required]
        public Guid ClientID { get; set; }

        public Guid? QuotationID { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public bool IsDesignByUs { get; set; }

        public decimal? DesigningCharge { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public int Status { get; set; }

        public int FirmID { get; set; }

        public int DesignStatus { get; set; }

        public string? CreatedBy { get; set; }

        public int? StateID { get; set; }
        public int? CityID { get; set; }

        public string? AssignedDesignTo { get; set; }
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(QuotationID))]
        public virtual Quotation? Quotation { get; set; }
        public bool EnableTax { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal GrandTotal { get; set; }

        public bool IsUseBillingAddress { get; set; }       
        public string? ShippingAddress { get; set; }
    }
}