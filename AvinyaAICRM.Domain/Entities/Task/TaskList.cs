
namespace AvinyaAICRM.Domain.Entities.Tasks
{
    public class TaskList
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Guid OwnerId { get; set; }
        public long? TeamId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskSeries> TaskSeries { get; set; }
    }

}
