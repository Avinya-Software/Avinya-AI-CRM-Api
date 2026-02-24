

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportClientDetails
    {
        public ClientInfo Client { get; set; }

        public List<ReportLeadDetailsdto> Leads { get; set; } = new();
        public int LeadCount { get; set; }

        public List<QuotationInfo> Quotations { get; set; } = new();
        public int QuotationCount { get; set; }
        public decimal TotalQuotationAmount { get; set; }

        public List<OrderInfo> Orders { get; set; } = new();
        public int OrderCount { get; set; }
        public decimal TotalOrderAmount { get; set; }

        public List<BillInfo> Bills { get; set; } = new();
        public int BillCount { get; set; }
        public decimal TotalBillAmount { get; set; }

        public List<OrderIteminfo> OrderItems { get; set; } = new();
        public int OrderItemCount { get; set; }

        public decimal GrandTotalSummary { get; set; }
    }


    public class BillInfo
    {
        public Guid BillID { get; set; }
        public string BillNo { get; set; }
        public Guid? OrderID { get; set; }
        public string StatusName { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Taxes { get; set; }
        public decimal? GrandTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal? DesigningCharge { get; set; }
        public decimal? RemainingPayment { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? PlaceOfSupply { get; set; }

        public bool? ReverseCharge { get; set; }

        public string? GRRRNo { get; set; }

        public DateTime? DueDate { get; set; }

        public string? Transport { get; set; }

        public string? VehicleNo { get; set; }

        public string? Station { get; set; }

        public string? EWayBillNo { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? Status { get; set; }

    }

}
