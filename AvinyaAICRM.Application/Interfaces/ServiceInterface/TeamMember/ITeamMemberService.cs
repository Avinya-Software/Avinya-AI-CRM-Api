using AvinyaAICRM.Application.DTOs.TeamMember;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.TeamMember
{
    public interface ITeamMemberService
    {
        Task<ResponseModel> GetMembersAsync(long teamId, string userId);
        Task<ResponseModel> AddMemberAsync(long teamId, AddTeamMemberDto dto, string userId);
        Task<ResponseModel> RemoveMemberAsync(long teamId, Guid memberId, string userId);
    }
}
