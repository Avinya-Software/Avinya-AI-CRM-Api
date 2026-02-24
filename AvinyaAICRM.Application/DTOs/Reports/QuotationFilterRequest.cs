

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class QuotationFilterRequest
    {
        public string? Search { get; set; }

        public string? StatusName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool GetAll { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
