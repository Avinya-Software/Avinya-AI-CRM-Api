namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class VendorFilterRequest
    {
        public string? Search { get; set; }
        public bool? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool GetAll { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

}
