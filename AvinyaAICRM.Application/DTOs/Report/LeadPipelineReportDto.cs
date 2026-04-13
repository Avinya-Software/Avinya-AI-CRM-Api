namespace AvinyaAICRM.Application.DTOs.Report
{
    // ─── KPI summary ───────────────────────────────────────────────────────────
    public class LeadPipelineKpiDto
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int OpenLeads { get; set; }
        public double ConversionRate { get; set; }   // percentage
        public double LossRate { get; set; }   // percentage
        public double AvgFollowUps { get; set; }   // avg per lead
    }

    // ─── Funnel by status ──────────────────────────────────────────────────────
    public class LeadFunnelItemDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }   // % of total leads
    }

    // ─── Source breakdown ──────────────────────────────────────────────────────
    public class LeadSourceBreakdownDto
    {
        public string SourceName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    // ─── Source conversion (acceptance rate per source) ───────────────────────
    public class LeadSourceConversionDto
    {
        public string SourceName { get; set; } = string.Empty;
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public double ConversionRate { get; set; }
    }

    // ─── Overdue follow-up row ─────────────────────────────────────────────────
    public class OverdueFollowUpDto
    {
        public string LeadNo { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime LastFollowUp { get; set; }
        public DateTime NextDue { get; set; }
        public int DaysOverdue { get; set; }
        public string FollowUpStatus { get; set; } = string.Empty;
    }

    // ─── Root response ─────────────────────────────────────────────────────────
    public class LeadPipelineReportDto
    {
        public LeadPipelineKpiDto Kpi { get; set; } = new();
        public List<LeadFunnelItemDto> Funnel { get; set; } = new();
        public List<LeadSourceBreakdownDto> SourceBreakdown { get; set; } = new();
        public List<LeadSourceConversionDto> SourceConversion { get; set; } = new();
        public List<OverdueFollowUpDto> OverdueFollowUps { get; set; } = new();

        // Applied filter snapshot — echoed back so frontend can display it
        public LeadPipelineFilterDto AppliedFilters { get; set; } = new();
    }
}
