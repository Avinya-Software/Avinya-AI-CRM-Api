

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportOrder
    {
        public Guid OrderID { get; set; }
        public string OrderNo { get; set; }

        public Guid? ClientID { get; set; }

        public string CompanyName { get; set; }

        public string ClientName { get; set; }

        public string QuotationNo { get; set; }

        public string ProductTotal { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsDesignByUs { get; set; }

        public decimal? DesigningCharge { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }

        public int DesignStatus { get; set; }

        public string DesignStatusName { get; set; } = string.Empty;

        public string AssignName { get; set; }

        public int OrderItemCount { get; set; }
        public decimal? OrderItemTotal { get; set; }

        public int QuotationItemCount{ get; set; }
        public decimal? QuotationTotal { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal GrandTotal { get; set; }

        public string WorkOrderVendors { get; set; }

        public string WorkOrderStatuses { get; set; }
        public int WorkOrderItemCount { get; set; }


    }
}
