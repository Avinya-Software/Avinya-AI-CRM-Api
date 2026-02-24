namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ClientFilterRequest
    {
        public string Search { get; set; }
        public string ClientType { get; set; }
        public bool? Status { get; set; }
        public string CreatedBy { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool GetAll { get; set; }
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }

}
