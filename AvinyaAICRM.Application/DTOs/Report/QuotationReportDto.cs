using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class QuotationReportKpiDto
    {
        public int TotalQuotations { get; set; }
        public int SentQuotations { get; set; }
        public int AcceptedQuotations { get; set; }
        public int RejectedQuotations { get; set; }
        public int ExpiredQuotations { get; set; }
        public int PendingQuotations { get; set; }
        public double AcceptanceRate { get; set; }   // %
        public double RejectionRate { get; set; }   // %
        public decimal TotalQuotedValue { get; set; }
        public decimal AcceptedValue { get; set; }
        public decimal RejectedValue { get; set; }
        public decimal AvgQuotationValue { get; set; }
    }

    // ─── Status breakdown ──────────────────────────────────────────────────────
    public class QuotationStatusBreakdownDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public double Percentage { get; set; }
    }

    // ─── Conversion rate by product ────────────────────────────────────────────
    public class QuotationProductBreakdownDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TimesQuoted { get; set; }
        public decimal TotalQuotedValue { get; set; }
        public int TimesConverted { get; set; }   // appeared in Accepted quotations
        public double ConversionRate { get; set; }   // %
    }

    // ─── Conversion rate by client ─────────────────────────────────────────────
    public class QuotationClientSummaryDto
    {
        public Guid ClientId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int TotalQuotations { get; set; }
        public int AcceptedQuotations { get; set; }
        public int RejectedQuotations { get; set; }
        public decimal TotalQuotedValue { get; set; }
        public decimal AcceptedValue { get; set; }
        public double AcceptanceRate { get; set; }
    }

    // ─── Expired / expiring quotations ─────────────────────────────────────────
    public class QuotationExpiryRowDto
    {
        public string QuotationNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public DateTime ValidTill { get; set; }
        public int DaysOverdue { get; set; }   // negative = expiring soon, positive = already expired
        public string StatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ─── Rejection analysis ────────────────────────────────────────────────────
    public class QuotationRejectionRowDto
    {
        public string QuotationNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public string RejectedNotes { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ─── Monthly trend ─────────────────────────────────────────────────────────
    public class QuotationMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalSent { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AcceptedValue { get; set; }
    }

    // ─── Root response ─────────────────────────────────────────────────────────
    public class QuotationReportDto
    {
        public QuotationReportKpiDto Kpi { get; set; } = new();
        public List<QuotationStatusBreakdownDto> StatusBreakdown { get; set; } = new();
        public List<QuotationProductBreakdownDto> ProductBreakdown { get; set; } = new();
        public List<QuotationClientSummaryDto> ClientSummary { get; set; } = new();
        public List<QuotationExpiryRowDto> ExpiryList { get; set; } = new();
        public List<QuotationRejectionRowDto> RejectionList { get; set; } = new();
        public List<QuotationMonthlyTrendDto> MonthlyTrend { get; set; } = new();
        public QuotationReportFilterDto AppliedFilters { get; set; } = new();
    }
}
