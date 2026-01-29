
namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class UpdateRecurringDto
    {
        public string RecurrenceRule { get; set; }   // RRULE
        public DateTime? EndDate { get; set; }
    }

}
