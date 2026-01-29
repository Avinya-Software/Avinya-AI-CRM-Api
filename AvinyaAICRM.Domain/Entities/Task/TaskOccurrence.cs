
namespace AvinyaAICRM.Domain.Entities.Tasks
{
    public class TaskOccurrence
    {
        public long Id { get; set; }

        public long TaskSeriesId { get; set; }
        public TaskSeries TaskSeries { get; set; }

        public long? ParentOccurrenceId { get; set; }
        public TaskOccurrence ParentOccurrence { get; set; }

        public DateTime? DueDateTime { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime? CompletedAt { get; set; }
        public string? SkippedReason { get; set; }

        public string? AssignedTo { get; set; }
        public long? RescheduledFromOccurrenceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
