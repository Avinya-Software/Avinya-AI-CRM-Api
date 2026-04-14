using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    // ─── KPI summary ───────────────────────────────────────────────────────────
    public class OrderReportKpiDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int ReadyOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int OverdueOrders { get; set; }    // past ExpectedDeliveryDate, not yet delivered
        public double DeliveryRate { get; set; }    // % delivered
        public double OnTimeDeliveryRate { get; set; }    // % delivered on or before expected date
        public decimal TotalOrderValue { get; set; }
        public decimal TotalInvoicedValue { get; set; }    // orders where isInvoiceCreated = true
        public decimal PendingInvoiceValue { get; set; }    // orders not yet invoiced
        public decimal AvgOrderValue { get; set; }
        public double AvgDaysToDeliver { get; set; }    // avg days from OrderDate to delivery
    }

    // ─── Order status breakdown ────────────────────────────────────────────────
    public class OrderStatusBreakdownDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public double Percentage { get; set; }
    }


    // ─── Top products in orders ────────────────────────────────────────────────
    public class OrderProductBreakdownDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalOrders { get; set; }   // distinct orders containing this product
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueShare { get; set; }   // % of total order value
    }

    // ─── Top clients by order count/value ──────────────────────────────────────
    public class OrderClientSummaryDto
    {
        public Guid ClientId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int OverdueOrders { get; set; }
        public decimal TotalValue { get; set; }
        public bool IsInvoiceCreated { get; set; }
    }


    // ─── Overdue orders list ───────────────────────────────────────────────────
    public class OrderOverdueRowDto
    {
        public string OrderNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public int DaysOverdue { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public bool IsInvoiceCreated { get; set; }
    }

    // ─── Orders pending invoice ────────────────────────────────────────────────
    public class OrderPendingInvoiceRowDto
    {
        public string OrderNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public int DaysSinceOrder { get; set; }
    }

    // ─── Monthly trend ─────────────────────────────────────────────────────────
    public class OrderMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int Delivered { get; set; }
        public int Overdue { get; set; }
        public decimal TotalValue { get; set; }
    }

    // ─── Root response ─────────────────────────────────────────────────────────
    public class OrderReportDto
    {
        public OrderReportKpiDto Kpi { get; set; } = new();
        public List<OrderStatusBreakdownDto> StatusBreakdown { get; set; } = new();
        public List<OrderProductBreakdownDto> ProductBreakdown { get; set; } = new();
        public List<OrderClientSummaryDto> ClientSummary { get; set; } = new();
        public List<OrderOverdueRowDto> OverdueList { get; set; } = new();
        public List<OrderPendingInvoiceRowDto> PendingInvoiceList { get; set; } = new();
        public List<OrderMonthlyTrendDto> MonthlyTrend { get; set; } = new();
        public OrderReportFilterDto AppliedFilters { get; set; } = new();
    }
}
