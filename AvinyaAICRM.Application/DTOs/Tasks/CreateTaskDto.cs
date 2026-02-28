
namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class CreateTaskDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }

        public long ListId { get; set; }
        public DateTime? DueDateTime { get; set; }

        // Recurring
        public bool IsRecurring { get; set; }
        public string? RecurrenceRule { get; set; } // RRULE string
        public DateTime? RecurrenceStartDate { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }

        // Reminder
        public DateTime? ReminderAt { get; set; }
        public string? ReminderChannel { get; set; } // Email / Push etc
        public long? TeamId  { get; set; }

        public string? AssignToId { get; set; }
        
        public string? ProjectId { get; set; } 

    }



}
