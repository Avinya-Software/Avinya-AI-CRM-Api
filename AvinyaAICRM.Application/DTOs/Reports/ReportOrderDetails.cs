

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportOrderDetails
    {
        public Guid OrderID { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public bool IsDesignByUs { get; set; }
        public decimal? DesigningCharge { get; set; }
        public string StatusName { get; set; }
        public string DesignStatusName { get; set; }
        public string AssignedDesignTo { get; set; }

        public ClientInfoDTO Client { get; set; }

        public QuotationInfoDTO Quotation { get; set; }

        public List<OrderItemDTO> OrderItems { get; set; }
        public decimal OrderItemTotal { get; set; }

        public List<WorkOrderDTO> WorkOrders { get; set; }
        public int WorkOrderItemCount { get; set; }

        public decimal GrandTotal { get; set; }

        public List<BillInfo> Bills { get; set; }
    }

    public class ClientInfoDTO
    {
        public Guid ClientID { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string BillingAddress { get; set; }
    }

    public class QuotationInfoDTO
    {
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; }
        public decimal? GrandTotal { get; set; }

        public DateTime QuotationDate { get; set; }

        public LeadInfoDTO Lead { get; set; }
        public List<QuotationItemDTO> QuotationItems { get; set; }
        public decimal QuotationItemTotal { get; set; }
    }

    public class LeadInfoDTO
    {
        public Guid LeadID { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string LeadSourceName { get; set; }
        public string StatusName { get; set; }

        public string RequirementDetails { get; set; }

        public DateTime LeadDate { get; set; }
    }

    public class OrderItemDTO
    {
        public Guid OrderItemID { get; set; }
        public string Description { get; set; }
        public decimal LineTotal { get; set; }
        public ProductInfoDTO Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ProductInfoDTO
    {
        public Guid ProductID { get; set; }
        public string ProductName { get; set; }
        public string TaxName { get; set; }
    }

    public class WorkOrderDTO
    {
        public Guid WorkOrderID { get; set; }
        public string WorkOrderNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string VendorName { get; set; }
        public string VendorContactPerson { get; set; }
        public string VendorEmail { get; set; }
        public string VendorMobile { get; set; }
        public string StatusName { get; set; }

        public DateTime WorkOrderDate { get; set; }



        public List<WorkOrderItemDTO> WorkOrderItems { get; set; }
    }

    public class WorkOrderItemDTO
    {
        public Guid WorkOrderItemID { get; set; }
        public string ProductDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string WorkTypeName { get; set; }
        public string ProcessStageName { get; set; }
    }

    public class QuotationItemDTO
    {
        public Guid QuotationItemID { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

    }
}
