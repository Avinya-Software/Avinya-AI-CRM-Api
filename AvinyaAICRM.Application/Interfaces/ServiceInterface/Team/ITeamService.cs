using AvinyaAICRM.Application.DTOs.Team;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Team
{
    public interface ITeamService
    {
        Task<ResponseModel> GetMyManagedTeamsAsync(string userId);
        Task<ResponseModel> GetByIdAsync(long id, string userId);
        Task<ResponseModel> CreateAsync(CreateTeamDto dto, string userId);
        Task<ResponseModel> UpdateAsync(long id, UpdateTeamDto dto, string userId);
        Task<ResponseModel> DeleteAsync(long id, string userId);
        Task<ResponseModel> GetDropdownAsync(string userId);
    }

}
