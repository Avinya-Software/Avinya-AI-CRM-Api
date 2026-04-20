using AvinyaAICRM.Application.DTOs.TeamMember;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.TeamMember;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.TeamMember;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.TeamMember
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly ITeamMemberRepository _repo;

        public TeamMemberService(ITeamMemberRepository repo)
        {
            _repo = repo;
        }

        public async Task<ResponseModel> GetMembersAsync(long teamId, string userId)
        {
            var result = await _repo.GetMembersAsync(teamId, userId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> AddMemberAsync(long teamId, AddTeamMemberDto dto, string userId)
        {
            var result = await _repo.AddMemberAsync(teamId, dto.UserId, userId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> RemoveMemberAsync(long teamId, Guid memberId, string userId)
        {
            var result = await _repo.RemoveMemberAsync(teamId, memberId, userId);
            return CommonHelper.GetResponseMessage(result);
        }
    }

}
