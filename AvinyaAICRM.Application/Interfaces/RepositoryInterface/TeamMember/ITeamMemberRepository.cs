using AvinyaAICRM.Application.DTOs.Team;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.TeamMember
{
    public interface ITeamMemberRepository
    {
        Task<List<TeamMemberDto>> GetMembersAsync(long teamId, string userId);
        Task<bool> AddMemberAsync(long teamId, Guid userId, string managerId);
        Task<bool> RemoveMemberAsync(long teamId, Guid userId, string managerId);
    }

}
