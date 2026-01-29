
namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class CreateTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public long ListId { get; set; }

        public DateTime? DueDateTime { get; set; }

        public bool IsRecurring { get; set; }
        public string RecurrenceRule { get; set; }

        public long? ParentTaskSeriesId { get; set; }
    }


}
