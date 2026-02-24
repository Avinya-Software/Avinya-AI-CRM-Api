

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class VendorPerformanceFilterRequest
    {
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool GetAll { get; set; } = false;

    }
}
