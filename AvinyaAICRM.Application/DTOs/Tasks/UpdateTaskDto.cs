
namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class UpdateTaskDto
    {
        public DateTime? DueDateTime { get; set; }
        public long? TeamId { get; set; }
        public string Status { get; set; }
        public string? AssignToId { get; set; }
    }

}
