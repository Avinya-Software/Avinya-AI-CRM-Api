using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.Clients
{
    public interface IClientRepository
    {
        Task<IEnumerable<ClientDropDownDto>> GetAllAsync(string tenantId, bool getAll = false);
        Task<ClientDto?> GetByIdAsync(Guid? ClientID, string tenantId);
        Task<Client> AddAsync(Client client);
        Task<Client?> UpdateAsync(ClientRequestDto clientDto, string tenantId);
        Task<bool> DeleteAsync(Guid id, string deletedBy, string tenantId);
        Task<PagedResult<ClientDto>> GetFilteredAsync(
     string? search,
     bool? status,
     int pageNumber,
     int pageSize,
     string userId);
        Task<(bool gstExists, bool mobileExists, bool emailExists)>
    CheckClientDuplicatesAsync(string? gst, string? mobile, string? email, Guid? excludeClientId = null);
    }
}
