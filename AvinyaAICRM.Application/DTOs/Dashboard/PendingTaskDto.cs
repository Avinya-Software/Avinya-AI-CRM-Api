namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class PendingTaskDto
    {
        public long OccurrenceId { get; set; }
        public string Title { get; set; }
        public DateTime? DueDateTime { get; set; }
    }
}
