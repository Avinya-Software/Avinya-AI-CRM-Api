using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.Clients
{
    public interface IClientRepository
    {
        Task<IEnumerable<ClientDropDownDto>> GetAllAsync(bool getAll = false);
        Task<ClientDto?> GetByIdAsync(Guid? ClientID);
        Task<Client> AddAsync(Client client);
        Task<Client?> UpdateAsync(ClientRequestDto clientDto);
        Task<bool> DeleteAsync(Guid id, string deletedBy);
        Task<PagedResult<ClientDto>> GetFilteredAsync(
     string? search,
     bool? status,
     int pageNumber,
     int pageSize);
        Task<(bool gstExists, bool mobileExists, bool emailExists)>
    CheckClientDuplicatesAsync(string? gst, string? mobile, string? email, Guid? excludeClientId = null);
    }
}
