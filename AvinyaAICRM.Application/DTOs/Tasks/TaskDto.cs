
namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class TaskDto
    {
        public long OccurrenceId { get; set; }
        public string Title { get; set; }
        public DateTime? DueDateTime { get; set; }
        public string Status { get; set; }
        public bool IsRecurring { get; set; }
    }

}
