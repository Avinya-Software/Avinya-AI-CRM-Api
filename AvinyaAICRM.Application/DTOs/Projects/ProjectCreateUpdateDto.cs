
namespace AvinyaAICRM.Application.DTOs.Projects
{
    public class ProjectCreateUpdateDto
    {
        public Guid? ProjectID { get; set; }

        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }

        public Guid? ClientID { get; set; }
        public string? Location { get; set; }

        public int Status { get; set; }
        public int Priority { get; set; }

        public int ProgressPercent { get; set; }

        public string? ProjectManagerId { get; set; }

        public string? AssignedToUserId { get; set; }
        public long? TeamId { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? Deadline { get; set; }

        public decimal? EstimatedValue { get; set; }
        public string? Notes { get; set; }
    }
}
