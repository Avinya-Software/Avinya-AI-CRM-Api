using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Client
{
    public interface IClientService
    {
        Task<ResponseModel> GetAllAsync(string tenantId, bool getAll = false);
        Task<ResponseModel> GetByIdAsync(Guid id, string tenantId);
        Task<ResponseModel> CreateAsync(ClientRequestDto dto, string userId);
        Task<ResponseModel> UpdateAsync(ClientRequestDto clientDto, string tenantId);
        Task<ResponseModel> DeleteAsync(Guid id, string deletedBy, string tenantId);
        Task<ResponseModel> GetFilteredAsync(string? search, bool? status, int page, int pageSize, string userId);

    }
}
