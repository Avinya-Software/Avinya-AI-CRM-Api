using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Invoice
{
    public class Invoice
    {
        [Key]
        public Guid InvoiceID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public string OrderID { get; set; } = string.Empty;
        public string ClientID { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal SubTotal { get; set; } = 0;
        public decimal Taxes { get; set; } = 0;
        public decimal Discount { get; set; } = 0;
        public decimal GrandTotal { get; set; } = 0;
        public int InvoiceStatusID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedDate { get; set; }
        public decimal RemainingPayment { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public string? PlaceOfSupply { get; set; }
        public bool ReverseCharge { get; set; } = false;
        public string? GRRRNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Transport { get; set; }
        public string? VehicleNo { get; set; }
        public string? Station { get; set; }
        public string? EWayBillNo { get; set; }
        public decimal AmountAfterDiscount { get; set; } = 0;
        public string TenantId { get; set; }
    }
}
