using System;
using System.Collections.Generic;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class QuotationLifecycleReportDto
    {
        public Guid QuotationID { get; set; }
        public string QuotationNo { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public string? ClientName { get; set; }
        public string? StatusName { get; set; }
        public decimal GrandTotal { get; set; }
        public string? CreatedBy { get; set; }

        public List<QuotationLifecycleItemDto> Items { get; set; } = new();
        public List<QuotationLifecycleOrderDto> Orders { get; set; } = new();
    }

    public class QuotationLifecycleItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class QuotationLifecycleOrderDto
    {
        public Guid OrderID { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal GrandTotal { get; set; }
        public string? StatusName { get; set; }
        public List<QuotationLifecycleInvoiceDto> Invoices { get; set; } = new();
    }

    public class QuotationLifecycleInvoiceDto
    {
        public Guid InvoiceID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingPayment { get; set; }
        public string? StatusName { get; set; }
    }
}
