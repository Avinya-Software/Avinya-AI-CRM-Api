
namespace AvinyaAICRM.Application.DTOs.Team
{
    public class TeamListDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
        public Guid ManagerId { get; set; }
        public string ManagerName { get; set; } = default!;

        public int TotalMembers { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TeamDetailsDto : TeamListDto
    {
        public List<TeamMemberDto> Members { get; set; } = new();
    }

    public class TeamMemberDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
    }

}
