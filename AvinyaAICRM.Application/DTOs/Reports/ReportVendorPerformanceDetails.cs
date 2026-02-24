

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportVendorPerformanceDetails
    {
        public Guid VendorID { get; set; }
        public string VendorName { get; set; }

        public string Mobile { get; set; }
        public string AlternateContact { get; set; }
        public string Email { get; set; }

        public string Address { get; set; }
        public string PaymentTerms { get; set; }
        public string PreferredPrintingTypes { get; set; }
        public DateTime CreatedDate { get; set; }

        public List<VendorPerformanceBlock> Performances { get; set; } = new();

        public class VendorPerformanceBlock
        {
            public VendorPerformanceDto VendorPerformance { get; set; }
            public WorkOrderDto WorkOrder { get; set; }
            public List<WorkOrderItemDto> WorkOrderItems { get; set; }
        }

        public class VendorPerformanceDto
        {
            public Guid PerformanceID { get; set; }
            public Guid WorkOrderID { get; set; }
            public string WorkOrderNo { get; set; }
            public bool OnTime { get; set; }
            public string DelayDays { get; set; }
            public string QualityRating { get; set; }
            public string Remark { get; set; }
            public DateTime EvaluationDate { get; set; }
        }

        public class WorkOrderDto
        {
            public Guid WorkOrderID { get; set; }
            public string WorkOrderNo { get; set; }
            public DateTime? DueDate { get; set; }
            public string Status { get; set; }
            public string StatusName { get; set; }
            public string Remark { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public class WorkOrderItemDto
        {
            public Guid WorkOrderItemID { get; set; }
            public Guid? OrderItemID { get; set; }
            public Guid ProductID { get; set; }
            public string ProductName { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public int WorkTypeID { get; set; }
            public string TypeName { get; set; }
            public int ProcessStage { get; set; }
            public string ProcessStageName { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }
}
