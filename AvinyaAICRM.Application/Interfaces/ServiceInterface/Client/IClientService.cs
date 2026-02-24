using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Client
{
    public interface IClientService
    {
        Task<ResponseModel> GetAllAsync(bool getAll = false);
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(ClientRequestDto dto);
        Task<ResponseModel> UpdateAsync(ClientRequestDto clientDto);
        Task<ResponseModel> DeleteAsync(Guid id, string deletedBy);
        Task<ResponseModel> GetFilteredAsync(string? search, bool? status, int page, int pageSize);

    }
}
