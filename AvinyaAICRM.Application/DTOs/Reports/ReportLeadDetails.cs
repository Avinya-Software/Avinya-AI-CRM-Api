
using System.ComponentModel.DataAnnotations;

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportLeadDetails
    {
        public ReportLeadDetailsdto Lead { get; set; }
        public ClientInfo Client { get; set; }
        public List<QuotationInfo> Quotations { get; set; } = new();
        public int QuotationCount { get; set; }
        public List<OrderInfo> Orders { get; set; } = new();
        public List<FollowupDetails> Followups { get; set; }
        public int FollowupCount { get; set; }
        public int OrderCount { get; set; }
    }

    public class ReportLeadDetailsdto
    {
        public Guid LeadID { get; set; }
        public string? LeadNo { get; set; }
        public string? CompanyName { get; set; }
        public string? RequirementDetails { get; set; }
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? LeadSourceName { get; set; }
        public string? StatusName { get; set; }
        public string? AssignToName { get; set; }
      

    }

    public class ClientInfo
    {
        public Guid ClientID { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? GSTNo { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public string? BillingAddress { get; set; }
    }

    public class OrderIteminfo
    {
        public Guid? OrderID { get; set; }
        public Guid? OrderItemId { get; set; }
        public string? OrderNo { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? TaxName { get; set; }
        public decimal? LineTotal { get; set; }
        public decimal? AllLineTotalSum { get; set; }
        public decimal? AllTaxSum { get; set; }
        public decimal? Grandtotal { get; set; }
        public DateTime OrderCreatedDate { get; set; }
    }

    public class QuotationInfo
    {
        public Guid QuotationID { get; set; }
        public string? QuotationNo { get; set; }
        public string? StatusName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Taxes { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime CreatedDate { get; set; }
        
    }

    public class OrderInfo
    {
        public Guid OrderID { get; set; }
        public string? OrderNo { get; set; }
        public string? StatusName { get; set; }
        public string? DesignStatusName { get; set; }
        public decimal? DesigningCharge { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal GrandTotal { get; set; }
        public string ShippingAddress { get; set; }
        public DateTime CreatedDate { get; set; }

    }

    public class FollowupDetails
    {
        public Guid LeadID { get; set; }
        public DateTime? NextFollowupDate { get; set; }
        public string? Notes { get; set; }
        public string? FollowUpBy { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }


}
