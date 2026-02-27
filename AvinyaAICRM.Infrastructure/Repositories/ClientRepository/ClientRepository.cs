using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Application.Interfaces.Clients;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Domain.Enums.Clients;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;


namespace AvinyaAICRM.Infrastructure.Repositories.ClientRepository
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<ClientDropDownDto>> GetAllAsync(string tenantId, bool getAll = false)
        {
            var query = _context.Clients
                .Where(c => c.TenantId.ToString() == tenantId)
                .AsQueryable();

            if (!getAll)
            {
                query = query.Where(c => c.Status == true && c.IsDeleted == false);
            }

            return await query
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new ClientDropDownDto
                {
                    ClientID = c.ClientID,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson,
                    Email = c.Email,
                    MobileNumber = c.Mobile,
                    StateID = c.StateID,
                    CityID = c.CityID,
                    GstNo = c.GSTNo,
                    BillAddress = c.BillingAddress,
                    ClientTypeName = ((ClientTypeEnum)c.ClientType).ToString(),
                    ClientType = c.ClientType
                })
                .ToListAsync();
        }

        public async Task<ClientDto?> GetByIdAsync(Guid? ClientID, string tenantId)
        {
            return await (
                from c in _context.Clients
                where !c.IsDeleted && c.ClientID == ClientID && c.TenantId.ToString() == tenantId
                join u in _context.Users on c.CreatedBy equals u.Id into users
                from user in users.DefaultIfEmpty()
                join s in _context.States on c.StateID equals s.StateID into states
                from state in states.DefaultIfEmpty()
                join ct in _context.Cities on c.CityID equals ct.CityID into cities
                from city in cities.DefaultIfEmpty()

                select new ClientDto
                {
                    ClientID = c.ClientID,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson,
                    Email = c.Email,
                    Mobile = c.Mobile,
                    GSTNo = c.GSTNo,
                    BillingAddress = c.BillingAddress,
                    StateID = c.StateID,
                    StateName = state != null ? state.StateName : null,
                    CityID = c.CityID,
                    CityName = city != null ? city.CityName : null,
                    Status = c.Status,
                    ClientType = c.ClientType,
                    ClientTypeName = c.ClientType != null
                        ? ((ClientTypeEnum)c.ClientType).ToString()
                        : null,
                    Notes = c.Notes,
                    CreatedBy = c.CreatedBy,
                    CreatedByName = user != null ? user.UserName : "Unknown",
                    CreatedDate = c.CreatedDate,
                    UpdatedAt = c.UpdatedAt
                }
            ).FirstOrDefaultAsync();
        }


        public async Task<Client> AddAsync(Client client)
        {
            var userData = await _context.Users.FindAsync(client.CreatedBy);
            client.TenantId = userData.TenantId;
            client.CreatedDate = DateTime.Now;

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();


            return client;
        }

        public async Task<Client?> UpdateAsync(ClientRequestDto clientDto, string tenantId)
        {
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(x => x.ClientID == clientDto.ClientID && !x.IsDeleted && x.TenantId.ToString() == tenantId);

            if (existingClient == null)
                return null;

            if (!string.IsNullOrWhiteSpace(clientDto.CompanyName))
                existingClient.CompanyName = clientDto.CompanyName.Trim();

            if (clientDto.ContactPerson != null)
                existingClient.ContactPerson = clientDto.ContactPerson.Trim();

            else
                existingClient.ContactPerson = string.Empty;


            if (clientDto.Mobile != null)
                existingClient.Mobile = clientDto.Mobile.Trim();
            else
                existingClient.Mobile = string.Empty;


            if (clientDto.Email != null)
                existingClient.Email = clientDto.Email.Trim();
            else
                existingClient.Email = string.Empty;


            if (clientDto.GSTNo != null)
                existingClient.GSTNo = clientDto.GSTNo.Trim();
            else
                existingClient.GSTNo = string.Empty;

            if (clientDto.BillingAddress != null)
                existingClient.BillingAddress = clientDto.BillingAddress.Trim();

            if (clientDto.StateID != null)
                existingClient.StateID = clientDto.StateID;
            else
                existingClient.StateID = 0;


            if (clientDto.CityID != null)
                existingClient.CityID = clientDto.CityID;
            else
                existingClient.CityID = 0;

            if (clientDto.ClientType > 0)
                existingClient.ClientType = clientDto.ClientType;

            existingClient.Status = clientDto.Status;

            if (clientDto.Notes != null)
                existingClient.Notes = clientDto.Notes.Trim();
            else
                existingClient.Notes = string.Empty;

            existingClient.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return existingClient;
        }

        public async Task<bool> DeleteAsync(Guid id, string deletedBy, string tenantId)
        {
            var existing = await _context.Clients
                .FirstOrDefaultAsync(x => x.ClientID == id
                           && x.TenantId.ToString() == tenantId);
            if (existing == null)
                return false;

            existing.IsDeleted = true;
            existing.DeletedBy = deletedBy;
            existing.DeletedDate = DateTime.Now;

            _context.Clients.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<ClientDto>> GetFilteredAsync(
     string? search,
     bool? status,
     int pageNumber,
     int pageSize
     ,string userId)
        {
            try
            {
                var userData = await _context.Users.FindAsync(userId);
                
                var query = _context.Clients
                    .Where(c => !c.IsDeleted && c.CreatedBy == userId && c.TenantId == userData.TenantId)
                    .AsQueryable();

                #region Search Filter

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var likeSearch = $"%{search}%";

                    query = query.Where(c =>
                        EF.Functions.Like(c.CompanyName, likeSearch) ||
                        EF.Functions.Like(c.ContactPerson, likeSearch) ||
                        EF.Functions.Like(c.Email, likeSearch));
                }

                #endregion

                #region Status Filter

                if (status.HasValue)
                    query = query.Where(c => c.Status == status.Value);

                #endregion

                // ✅ Total Records
                var totalRecords = await query.CountAsync();

                // ✅ Query with LEFT JOINS + DTO Mapping
                var data = await (
                    from c in query

                    join u in _context.Users
                        on c.CreatedBy equals u.Id into users
                    from user in users.DefaultIfEmpty()

                    join s in _context.States
                        on c.StateID equals s.StateID into states
                    from state in states.DefaultIfEmpty()

                    join ct in _context.Cities
                        on c.CityID equals ct.CityID into cities
                    from city in cities.DefaultIfEmpty()

                    orderby c.CreatedDate descending

                    select new ClientDto
                    {
                        ClientID = c.ClientID,
                        CompanyName = c.CompanyName,
                        ContactPerson = c.ContactPerson,
                        Mobile = c.Mobile,
                        Email = c.Email,
                        GSTNo = c.GSTNo,
                        BillingAddress = c.BillingAddress,

                        StateID = c.StateID,
                        StateName = state != null ? state.StateName : null,

                        CityID = c.CityID,
                        CityName = city != null ? city.CityName : null,

                        ClientType = c.ClientType,
                        ClientTypeName = c.ClientType != null
                            ? ((ClientTypeEnum)c.ClientType).ToString()
                            : null,

                        Status = c.Status,
                        Notes = c.Notes,

                        CreatedBy = c.CreatedBy,
                        CreatedByName = user != null ? user.UserName : null,

                        CreatedDate = c.CreatedDate,
                        UpdatedAt = c.UpdatedAt
                    })
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                // ✅ Return Standard Paging Object
                return new PagedResult<ClientDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    Data = data
                };
            }
            catch
            {
                throw;
            }
        }


        public async Task<(bool gstExists, bool mobileExists, bool emailExists)>
    CheckClientDuplicatesAsync(string? gst, string? mobile, string? email, Guid? excludeClientId = null)
        {
            var query = _context.Clients.AsQueryable();

            if (excludeClientId.HasValue)
                query = query.Where(x => x.ClientID != excludeClientId.Value);

            bool gstExists = !string.IsNullOrEmpty(gst) &&
                             await query.AnyAsync(x => x.GSTNo == gst);

            bool mobileExists = !string.IsNullOrEmpty(mobile) &&
                                await query.AnyAsync(x => x.Mobile == mobile);

            bool emailExists = !string.IsNullOrEmpty(email) &&
                               await query.AnyAsync(x => x.Email == email);

            return (gstExists, mobileExists, emailExists);
        }




    }
}
