

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportVendorPerformance
    {
        public Guid VendorID { get; set; }

        public string VendorName { get; set; }

        public string WorkOrderCount { get; set; }
        public string OnTimeCount { get; set; }
        public string DelayCount { get; set; }

        public decimal QuotationTotal { get; set; }
        public decimal OrderSubTotal { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal OrderGrandTotal { get; set; }
        public decimal DesigningCharge { get; set; }

        public decimal PerformanceRating { get; set; }
        public DateTime EvaluationDate { get; set; }
    }
}
