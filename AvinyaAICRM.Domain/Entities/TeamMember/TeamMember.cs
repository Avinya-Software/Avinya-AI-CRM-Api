
namespace AvinyaAICRM.Domain.Entities.TeamMember
{
    public class TeamMember
    {
        public long Id { get; set; }
        public long TeamId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

}
