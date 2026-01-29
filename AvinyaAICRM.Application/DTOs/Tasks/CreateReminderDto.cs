
namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class CreateReminderDto
    {
        public string TriggerType { get; set; }
        public int OffsetMinutes { get; set; }
        public string Channel { get; set; }
    }

}
