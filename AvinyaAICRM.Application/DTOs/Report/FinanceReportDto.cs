using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    // ══════════════════════════════════════════════════════════════════════════
    // SECTION A — OVERALL FINANCIAL SUMMARY
    // ══════════════════════════════════════════════════════════════════════════

    public class FinanceSummaryKpiDto
    {
        // Revenue side
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOverdue { get; set; }
        public double CollectionRate { get; set; }   // % of invoiced that is collected

        // Expense side
        public decimal TotalExpenses { get; set; }

        // Net position
        public decimal NetPosition { get; set; }   // TotalCollected - TotalExpenses
        public decimal GrossProfitMargin { get; set; }   // ((Collected - Expenses) / Collected) * 100

        // Counts
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int PartialInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public int OverdueInvoices { get; set; }
        public int TotalPaymentRecords { get; set; }
        public int TotalExpenseRecords { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SECTION B — INVOICE
    // ══════════════════════════════════════════════════════════════════════════

    public class InvoiceStatusBreakdownDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public double Percentage { get; set; }
    }

    public class InvoiceAgingBucketDto
    {
        public string Bucket { get; set; } = string.Empty;   // "0-15 days", "16-30 days", etc.
        public int Count { get; set; }
        public decimal Outstanding { get; set; }
        public double Percentage { get; set; }   // % of total outstanding
    }

    public class InvoiceClientOutstandingDto
    {
        public Guid ClientId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int TotalInvoices { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public decimal Overdue { get; set; }
    }

    public class InvoiceOverdueRowDto
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public decimal Outstanding { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SECTION C — PAYMENTS
    // ══════════════════════════════════════════════════════════════════════════

    public class PaymentModeBreakdownDto
    {
        public string PaymentMode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }   // % of total collected
    }

    public class PaymentMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetPosition { get; set; }
    }

    public class RecentPaymentRowDto
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string TransactionRef { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SECTION D — EXPENSES
    // ══════════════════════════════════════════════════════════════════════════

    public class ExpenseCategoryBreakdownDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }   // % of total expenses
    }

    public class ExpensePaymentModeBreakdownDto
    {
        public string PaymentMode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }

    public class ExpenseMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }

    public class TopExpenseRowDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string AddedBy { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ROOT RESPONSE
    // ══════════════════════════════════════════════════════════════════════════

    public class FinanceReportDto
    {
        // A — Combined summary
        public FinanceSummaryKpiDto Summary { get; set; } = new();

        // B — Invoice sections
        public List<InvoiceStatusBreakdownDto> InvoiceStatusBreakdown { get; set; } = new();
        public List<InvoiceAgingBucketDto> InvoiceAging { get; set; } = new();
        public List<InvoiceClientOutstandingDto> ClientOutstanding { get; set; } = new();
        public List<InvoiceOverdueRowDto> OverdueInvoices { get; set; } = new();

        // C — Payment sections
        public List<PaymentModeBreakdownDto> PaymentModeBreakdown { get; set; } = new();
        public List<RecentPaymentRowDto> RecentPayments { get; set; } = new();

        // D — Expense sections
        public List<ExpenseCategoryBreakdownDto> ExpenseCategoryBreakdown { get; set; } = new();
        public List<ExpensePaymentModeBreakdownDto> ExpensePaymentBreakdown { get; set; } = new();
        public List<ExpenseMonthlyTrendDto> ExpenseMonthlyTrend { get; set; } = new();
        public List<TopExpenseRowDto> TopExpenses { get; set; } = new();

        // Combined monthly trend (invoiced + collected + expenses + net)
        public List<PaymentMonthlyTrendDto> MonthlyTrend { get; set; } = new();

        public FinanceReportFilterDto AppliedFilters { get; set; } = new();
    }
}
