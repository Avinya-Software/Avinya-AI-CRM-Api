using AvinyaAICRM.Application.DTOs.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Team;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Team
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _repo;

        public TeamService(ITeamRepository repo)
        {
            _repo = repo;
        }

        public async Task<ResponseModel> GetMyManagedTeamsAsync(string userId)
        {
            var result = await _repo.GetMyManagedTeamsAsync(userId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetByIdAsync(long id, string userId)
        {
            var result = await _repo.GetByIdAsync(id, userId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> CreateAsync(CreateTeamDto dto, string userId)
        {
            var result = await _repo.CreateAsync(dto, userId);
            return result;
        }

        public async Task<ResponseModel> UpdateAsync(long id, UpdateTeamDto dto, string userId)
        {
            var result = await _repo.UpdateAsync(id, dto, userId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> DeleteAsync(long id, string userId)
        {
            var result = await _repo.DeleteAsync(id, userId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetDropdownAsync(string userId)
        {
            var teams = await _repo.GetDropdownAsync(userId);

            return CommonHelper.GetResponseMessage(teams);
        }
    }
}
