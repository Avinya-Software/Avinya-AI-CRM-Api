

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class LeadFilterRequest
    {
        public string? Search { get; set; }
        public string? ClientName  { get; set; }
        public string? StatusName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool GetAll { get; set; } = false;
    }
}
