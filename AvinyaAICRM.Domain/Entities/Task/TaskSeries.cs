
namespace AvinyaAICRM.Domain.Entities.Tasks
{
    public class TaskSeries
    {
        public long Id { get; set; }

        public long? ParentTaskSeriesId { get; set; }
        public TaskSeries ParentTaskSeries { get; set; }

        public long ListId { get; set; }
        public TaskList List { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }

        public bool IsRecurring { get; set; }
        public string? RecurrenceRule { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string CreatedBy { get; set; }
        public long? TeamId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? TaskScope { get; set; }

        public string? Priority { get; set; }
        public Guid? ProjectId { get; set; }

        public ICollection<TaskOccurrence> Occurrences { get; set; }
    }

}
