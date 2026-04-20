using System;
using System.Collections.Generic;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class OrderLifecycleReportDto
    {
        public Guid OrderID { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string? ClientName { get; set; }
        public string? StatusName { get; set; }
        public decimal GrandTotal { get; set; }

        public List<LifecycleOrderItemDto> Items { get; set; } = new();
        public List<OrderInvoiceDto> Invoices { get; set; } = new();
        public List<OrderPaymentDto> Payments { get; set; } = new();
    }

    public class LifecycleOrderItemDto
    {
        public Guid ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class OrderInvoiceDto
    {
        public Guid InvoiceID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public string? StatusName { get; set; }
    }

    public class OrderPaymentDto
    {
        public Guid PaymentID { get; set; }
        public Guid InvoiceID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMode { get; set; }
        public string? TransactionRef { get; set; }
    }
}
