

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportDispatchDetails
    {
        public Guid DispatchID { get; set; }

        public OrderData order { get; set; }

        public DateTime? DispatchDate { get; set; }

        public int DispatchModeID { get; set; }

        public string DispatchModeName { get; set; }

        public string TrackingNo { get; set; }

        public string DispatchedBy { get; set; }

        public string DispatchedName { get; set; }

        public bool ItemsDispatched { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public class OrderData
    {
        public Guid OrderId { get; set; }

        public string OrderNo { get; set; }

        public Guid ClientId { get; set; }

        public QuotationsData Quotations { get; set; }

        public DateTime OrderDate { get; set; }

        public bool IsDesignByUs { get; set; }

        public decimal DesigningCharge { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal GrandTotal { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public int Status { get; set; }

        public string StatusName { get; set; }

        public int DesignStatus { get; set; }

        public string DesignStatusName { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<OrderItemData> Orderitems { get; set; } = new();

    }

    public class QuotationsData
    {
        public Guid QuotationID { get; set; }

        public string QuotationNo { get; set; }

        public Guid? LeadID { get; set; }

        public string LeadContactPerson { get; set; }

        public DateTime QuotationDate { get; set; }

        public DateTime ValidTill { get; set; }

        public Guid Status { get; set; }

        public string StatusName { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal Taxes { get; set; }

        public decimal GrandTotal { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<QuotationsItemData> Quotationsitems { get; set; } = new();

    }

    public class OrderItemData
    {
        public Guid OrderItemID { get; set; }

        public Guid? ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public Guid? TaxCategoryID { get; set; }

        public string TaxName { get; set; }

        public decimal Rate { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class QuotationsItemData
    {
        public Guid QuotationItemID { get; set; }

        public Guid? ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public Guid? TaxCategoryID { get; set; }

        public string TaxName { get; set; }

        public decimal LineTotal { get; set; }
    }
}
