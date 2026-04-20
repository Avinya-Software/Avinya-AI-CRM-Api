
namespace AvinyaAICRM.Domain.Entities.Tasks
{
    public class NotificationRule
    {
        public long Id { get; set; }

        public long TaskOccurrenceId { get; set; }
        public TaskOccurrence TaskOccurrence { get; set; }

        public string TriggerType { get; set; }
        public int OffsetMinutes { get; set; }
        public string Channel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
