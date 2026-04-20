using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Client
{
    public interface IClientService
    {
        Task<ResponseModel> GetAllAsync(string tenantId, string? role, bool getAll = false);
        Task<ResponseModel> GetByIdAsync(Guid clientId, string? tenantId, string? role);
        Task<ResponseModel> CreateAsync(ClientRequestDto dto, string userId);
        Task<ResponseModel> UpdateAsync(ClientRequestDto dto, string? tenantId, string? role);
        Task<ResponseModel> DeleteAsync(Guid id, string deletedBy, string tenantId, string? role);
        Task<ResponseModel> GetFilteredAsync(string? search, bool? status, int page, int pageSize, string userId);

    }
}
