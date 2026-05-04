using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.Clients
{
    public interface IClientRepository
    {
        Task<IEnumerable<ClientDropDownDto>> GetAllAsync(string tenantId, string? role, bool getAll = false);
        Task<ClientDto?> GetByIdAsync(Guid? ClientID, string? tenantId, string? role);
        Task<Client> AddAsync(Client client);
        Task<Client?> UpdateAsync(ClientRequestDto clientDto, string? tenantId, string? role);
        Task<bool> DeleteAsync(Guid id, string deletedBy, string tenantId, string? role);
        Task<PagedResult<ClientDto>> GetFilteredAsync(
     string? search,
     bool? status,
     int pageNumber,
     int pageSize,
     string userId,
     string? role);
        Task<(bool gstExists, bool mobileExists, bool emailExists)>
    CheckClientDuplicatesAsync(string? gst, string? mobile, string? email, Guid? excludeClientId = null);
        Task<IEnumerable<Client>> FindByNameAsync(string name, Guid tenantId);
        Task<IEnumerable<Client>> FindByContactPersonAsync(string name, Guid tenantId);
        Task<Client?> FindByNameAndMobileAsync(string name, string mobile, Guid tenantId);
    }
}
