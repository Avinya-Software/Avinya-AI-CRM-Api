

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class DispatchFilterRequest
    {
        public string? Search { get; set; }
        public string? DispatchedStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool GetAll { get; set; } = false;

    }
}
