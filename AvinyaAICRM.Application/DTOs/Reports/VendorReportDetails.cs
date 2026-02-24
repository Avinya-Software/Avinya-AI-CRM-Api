
namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class VendorReportDetails
    {
        public VendorDetailsDto Vendor { get; set; }
        public int WorkOrderCount { get; set; }
        public int WorkOrderItemCount { get; set; }

        public List<InwardInfo> Inwards { get; set; }
        public int InwardCount { get; set; }

        public List<VendorPerformanceInfo> Performance { get; set; }
        public int PerformanceCount { get; set; }
        public decimal TotalWorkOrderAmount { get; set; }
        public int TotalInwardQuantity { get; set; }
        public int TotalWorkOrderQuantity { get; set; }
        public int TotalPendingQuantity { get; set; }
    }

    public class VendorDetailsDto
    {
        public Guid VendorID { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string AlternateContact { get; set; }
        public string Email { get; set; }
        public string GSTNo { get; set; }
        public string Address { get; set; }
        public int? CityID { get; set; }
        public int? StateID { get; set; }
        public string Pincode { get; set; }
        public string PaymentTerms { get; set; }
        public string PreferredPrintingTypes { get; set; }
        public bool Status { get; set; }

        public string CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class WorkOrderInfo
    {
        public Guid WorkOrderID { get; set; }
        public string? VendorName { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedDate { get; set; }

        public List<WorkOrderItemInfo>? WorkOrderItems { get; set; }

        public decimal WorkOrderLineTotalSum { get; set; }
    }

    public class WorkOrderItemInfo
    {
        public Guid WorkOrderItemID { get; set; }
        public DateTime? WorkOrderCreatedDate { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal AllLineTotalSum { get; set; }
    }

    public class InwardInfo
    {
        public Guid InwardID { get; set; }
        public DateTime InwardDate { get; set; }
        public decimal QuantityReceived { get; set; }
    }

    public class VendorPerformanceInfo
    {
        public Guid VPID { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}
