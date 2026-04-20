using AvinyaAICRM.Application.DTOs.Team;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team
{
    public interface ITeamRepository
    {
        Task<List<TeamListDto>> GetMyManagedTeamsAsync(string userId);
        Task<TeamDetailsDto?> GetByIdAsync(long id, string userId);
        Task<ResponseModel> CreateAsync(CreateTeamDto dto, string userId);
        Task<TeamDto?> UpdateAsync(long id, UpdateTeamDto dto, string userId);
        Task<bool> DeleteAsync(long id, string userId);
        Task<List<TeamDropdownDto>> GetDropdownAsync(string userId);
        Task<long?> ResolveTeamId(string userId, string? teamName);
    }

}
