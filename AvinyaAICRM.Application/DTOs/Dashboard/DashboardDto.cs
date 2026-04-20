namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class DashboardDto
    {
        public DashboardCounts Counts { get; set; }

        public ClientSummaryDto ClientSummary { get; set; }

        public int OverdueFollowupsCount { get; set; }

        public int PendingQuotationsCount { get; set; }

        public int TotalOrdersCount { get; set; }

        public int PendingOrdersCount { get; set; }
       public TodayActionDto TodayActions { get; set; }

        public List<RecentOrderDto> RecentOrders { get; set; }

        public List<RecentQuotationDto> RecentQuotations { get; set; }

        public List<UpcomingFollowupDto> UpcomingFollowups { get; set; }

        public List<PendingTaskDto> PendingTasks { get; set; }

        public List<HotLeadDto> HotLeads { get; set; }

        public List<AttentionDto> NeedsAttention { get; set; }

        public List<string> Suggestions { get; set; }
    }
}
