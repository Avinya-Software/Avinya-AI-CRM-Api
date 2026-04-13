namespace AvinyaAICRM.Application.DTOs.Report
{
    public class LeadPipelineFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public Guid? LeadSourceId { get; set; }
        public Guid? LeadStatusId { get; set; }
        public string? AssignedTo { get; set; }
        public Guid TenantId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}