

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportDispatch
    {
        public Guid DispatchID { get; set; }
        public Guid OrderID { get; set; }
        public decimal DesigningChnage { get; set; }

        public decimal OrderSubTotal { get; set; }
        public decimal OrderTotalTaxes { get; set; }
        public decimal OrderGrandTotal { get; set; }
        public int OrderItemCount { get; set; }
        public Guid QuotationID { get; set; }
        public decimal QuotationItemAmount { get; set; }

        public int DispatchModeID { get; set; }
        public string DispatchModeName { get; set; }
    }
}
