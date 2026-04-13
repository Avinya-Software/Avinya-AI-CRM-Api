using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class ClientReportKpiDto
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOverdue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalOrders { get; set; }
    }

    // ─── Top clients by revenue ────────────────────────────────────────────────
    public class ClientRevenueItemDto
    {
        public Guid ClientId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Outstanding { get; set; }
        public decimal RevenueShare { get; set; }   // % of total invoiced
    }

    // ─── Client 360 — per client drill-down ────────────────────────────────────
    public class Client360Dto
    {
        public Guid ClientId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GSTNo { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;

        // Activity counts
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int TotalQuotations { get; set; }
        public int AcceptedQuotations { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProjects { get; set; }

        // Financials
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Outstanding { get; set; }
        public decimal TotalExpectedValue { get; set; }   // sum of project EstimatedValue

        // Payment behaviour
        public string PreferredPaymentMode { get; set; } = string.Empty;
        public double AvgDaysToPayment { get; set; }

        // Last activity dates
        public DateTime? LastOrderDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? LastLeadDate { get; set; }
    }

    // ─── Outstanding aging row ─────────────────────────────────────────────────
    public class ClientAgingRowDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public decimal Outstanding { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public string InvoiceStatus { get; set; } = string.Empty;
    }

    // ─── State-wise revenue ────────────────────────────────────────────────────
    public class StateRevenueDto
    {
        public string StateName { get; set; } = string.Empty;
        public int ClientCount { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal Percentage { get; set; }
    }

    // ─── Root response ─────────────────────────────────────────────────────────
    public class ClientReportDto
    {
        public ClientReportKpiDto Kpi { get; set; } = new();
        public List<ClientRevenueItemDto> TopClients { get; set; } = new();
        public List<Client360Dto> Client360 { get; set; } = new();
        public List<ClientAgingRowDto> AgingList { get; set; } = new();
        public List<StateRevenueDto> StateBreakdown { get; set; } = new();
        public ClientReportFilterDto AppliedFilters { get; set; } = new();
    }
}
