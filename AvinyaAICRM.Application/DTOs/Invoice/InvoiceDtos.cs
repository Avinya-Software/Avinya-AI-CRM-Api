using AvinyaAICRM.Application.DTOs.Order;
using System;

namespace AvinyaAICRM.Application.DTOs.Invoice
{
    public class InvoiceDto
    {
        public Guid InvoiceID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public string OrderID { get; set; } = string.Empty;
        public string OrderNO { get; set; }
        public string ClientID { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Taxes { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public int InvoiceStatusID { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal RemainingPayment { get; set; }
        public decimal PaidAmount { get; set; }
        public string? PlaceOfSupply { get; set; }
        public bool ReverseCharge { get; set; }
        public string? GRRRNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Transport { get; set; }
        public string? VehicleNo { get; set; }
        public string? Station { get; set; }
        public string? EWayBillNo { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactPerson { get; set; }
        public string? StatusName { get; set; }
        public int? TotalCount { get; set; }
        public decimal AmountAfterDiscount { get; set; }
        public List<OrderItemReponceDto>? OrderItems { get; set; }
    }

    public class CreateInvoiceDto
    {
        public string OrderID { get; set; } = string.Empty;
        public string ClientID { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal SubTotal { get; set; }
        public decimal Taxes { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public int InvoiceStatusID { get; set; }
        public string? PlaceOfSupply { get; set; }
        public bool ReverseCharge { get; set; }
        public string? GRRRNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Transport { get; set; }
        public string? VehicleNo { get; set; }
        public string? Station { get; set; }
        public string? EWayBillNo { get; set; }
    }

    public class UpdateInvoiceDto
    {
        public Guid InvoiceID { get; set; }
        public string OrderID { get; set; } = string.Empty;
        public string ClientID { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Taxes { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public int InvoiceStatusID { get; set; }
        public string? PlaceOfSupply { get; set; }
        public bool ReverseCharge { get; set; }
        public string? GRRRNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Transport { get; set; }
        public string? VehicleNo { get; set; }
        public string? Station { get; set; }
        public string? EWayBillNo { get; set; }
    }
}
