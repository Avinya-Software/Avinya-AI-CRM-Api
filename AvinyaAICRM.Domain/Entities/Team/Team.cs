
namespace AvinyaAICRM.Domain.Entities.Team
{
    public class Team
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid TenantId { get; set; }
        public Guid ManagerId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
