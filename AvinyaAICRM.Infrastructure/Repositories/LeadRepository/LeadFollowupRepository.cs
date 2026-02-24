using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.LeadRepository
{
    public class LeadFollowupRepository : ILeadFollowupRepository
    {
        private readonly AppDbContext _context;

        public LeadFollowupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LeadFollowupStatus>> GetLeadFollowupStatusAsync()
        {
            return await _context.LeadFollowupStatuses.ToListAsync();
        }


        //Get All Follow-ups
        public async Task<IEnumerable<LeadFollowupDto>> GetAllAsync()
        {
            var leads = await _context.Leads
                .Select(l => new { l.LeadID, l.LeadNo })
                .ToListAsync();

            var users = await _context.Users
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var statuses = await _context.LeadFollowupStatuses
                .Select(s => new { s.LeadFollowupStatusID, s.StatusName })
                .ToListAsync();

            var followups = await _context.LeadFollowups
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            DateTime ConvertUtcToLocal(DateTime utcDate) =>
                TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            return followups.Select(f =>
            {
                var lead = leads.FirstOrDefault(l => l.LeadID == f.LeadID);
                var user = users.FirstOrDefault(u => u.Id == f.FollowUpBy);
                var status = statuses.FirstOrDefault(s => s.LeadFollowupStatusID == f.Status);

                return new LeadFollowupDto
                {
                    FollowUpID = f.FollowUpID,
                    LeadID = f.LeadID,
                    LeadNo = lead?.LeadNo,
                    Notes = f.Notes,
                    NextFollowupDate = f.NextFollowupDate,
                    Status = f.Status,
                    StatusName = status?.StatusName,
                    FollowUpBy = f.FollowUpBy,
                    FollowUpByName = user?.UserName,
                    CreatedDate = f.CreatedDate.HasValue ? ConvertUtcToLocal(f.CreatedDate.Value) : (DateTime?)null,
                };
            }).ToList();
        }


        // Get by ID
        public async Task<LeadFollowupDto?> GetByIdAsync(Guid id)
        {
            var f = await _context.LeadFollowups
                .FirstOrDefaultAsync(x => x.FollowUpID == id);

            if (f == null) return null;

            var statuses = await _context.LeadFollowupStatuses
                .Select(s => new { s.LeadFollowupStatusID, s.StatusName })
                .ToListAsync();

            var clients = await _context.Clients
                .Select(c => new
                {
                    c.ClientID,
                    c.ContactPerson,
                    c.CompanyName,
                    c.Mobile,
                    c.Email
                })
                .ToListAsync();


            var lead = await _context.Leads
                .Where(l => l.LeadID == f.LeadID)
                .Select(l => new
                {
                    l.LeadNo,
                    l.ClientID
                })
                .FirstOrDefaultAsync();

            var leadNo = lead?.LeadNo;
            var clientId = lead?.ClientID;


            var client = clients.FirstOrDefault(c => c.ClientID == clientId);

            var userName = !string.IsNullOrWhiteSpace(f.FollowUpBy)
                ? await _context.Users
                    .Where(u => u.Id == f.FollowUpBy)
                    .Select(u => u.UserName)
                    .FirstOrDefaultAsync()
                : null;

            return new LeadFollowupDto
            {
                FollowUpID = f.FollowUpID,
                LeadID = f.LeadID,
                LeadNo = leadNo,
                Notes = f.Notes,
                NextFollowupDate = f.NextFollowupDate,
                Status = f.Status,
                StatusName = statuses
                    .FirstOrDefault(s => s.LeadFollowupStatusID == f.Status)?.StatusName,
                FollowUpBy = f.FollowUpBy,
                FollowUpByName = userName,
                CreatedDate = f.CreatedDate,

                ClientName = client?.ContactPerson,
                CompanyName = client?.CompanyName,
                Mobile = client?.Mobile,
                Email = client?.Email
            };
        }



        //  Create
        public async Task<(LeadFollowups? Data, string? Error)> AddAsync(LeadFollowups dto)
        {
            // Check if lead exists
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadID == dto.LeadID);
            if (lead == null)
                return (null, "Lead not found");

            // Latest follow-up
            var latestFollowup = await _context.LeadFollowups
                .Where(f => f.LeadID == dto.LeadID)
                .OrderByDescending(f => f.CreatedDate)
                .FirstOrDefaultAsync();

            // Block if pending/in-progress
            if (latestFollowup != null && (latestFollowup.Status == 1 || latestFollowup.Status == 2))
                return (null, "A follow-up is already pending or in progress for this lead.");

            var newFollowup = new LeadFollowups
            {
                FollowUpID = Guid.NewGuid(),
                LeadID = dto.LeadID,
                Notes = dto.Notes,
                NextFollowupDate = dto.NextFollowupDate,
                Status = 1,
                FollowUpBy = dto.FollowUpBy
            };


            await _context.LeadFollowups.AddAsync(newFollowup);
            await _context.SaveChangesAsync();

            var result = new LeadFollowups
            {
                FollowUpID = newFollowup.FollowUpID,
                LeadID = newFollowup.LeadID,
                Notes = newFollowup.Notes,
                NextFollowupDate = newFollowup.NextFollowupDate,
                Status = newFollowup.Status,
                FollowUpBy = newFollowup.FollowUpBy,
                CreatedDate = newFollowup.CreatedDate
            };

            return (result, null);
        }




        // ✅ Update
        public async Task<LeadFollowups?> UpdateAsync(LeadFollowups dto)
        {
            var existing = await _context.LeadFollowups
                .FirstOrDefaultAsync(f => f.FollowUpID == dto.FollowUpID);

            if (existing == null)
                return null;

            // ❗ RULE: If THIS follow-up is already completed, block update
            if (existing.Status == 3)
                return null;

            // Update NOTES
            if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes != "string")
                existing.Notes = dto.Notes;

            // Update STATUS only if not completed
            if (dto.Status > 0)
                existing.Status = dto.Status;

            if (!string.IsNullOrWhiteSpace(dto.FollowUpBy) && dto.FollowUpBy != "string")
                existing.FollowUpBy = dto.FollowUpBy;
            // Update NextFollowupDate
            if (dto.NextFollowupDate.HasValue)
                existing.NextFollowupDate = dto.NextFollowupDate;

            existing.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return existing;
        }

        // ✅ Delete
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.LeadFollowups.FindAsync(id);
            if (entity == null) return false;

            _context.LeadFollowups.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        //  Filter + Pagination
        public async Task<PagedResult<LeadFollowupDto>> GetFilteredAsync(
     string? search,
     string? status,
     Guid? leadId,
     int pageNumber,
     int pageSize)
        {
            var query =
                from f in _context.LeadFollowups
                join l in _context.Leads on f.LeadID equals l.LeadID into leadGroup
                from lead in leadGroup.DefaultIfEmpty()

                join c in _context.Clients on lead.ClientID equals c.ClientID into clientGroup
                from client in clientGroup.DefaultIfEmpty()

                join ls in _context.LeadFollowupStatuses
                    on f.Status equals ls.LeadFollowupStatusID into statusGroup
                from statusName in statusGroup.DefaultIfEmpty()

                join u in _context.Users on f.FollowUpBy equals u.Id into userGroup
                from user in userGroup.DefaultIfEmpty()

                select new { f, lead, client, user, statusName };

            #region Lead Filter

            if (leadId.HasValue && leadId != Guid.Empty)
                query = query.Where(x => x.f.LeadID == leadId);

            #endregion

            #region Search Filter

            if (!string.IsNullOrWhiteSpace(search))
            {
                var likeSearch = $"%{search.ToLower()}%";

                query = query.Where(x =>
                    EF.Functions.Like(x.f.Notes.ToLower(), likeSearch) ||
                    EF.Functions.Like(x.lead.LeadNo.ToLower(), likeSearch) ||
                    EF.Functions.Like(x.client.ContactPerson.ToLower(), likeSearch) ||
                    EF.Functions.Like(x.client.Mobile.ToLower(), likeSearch) ||
                    EF.Functions.Like(x.client.Email.ToLower(), likeSearch) ||
                    EF.Functions.Like(x.user.UserName.ToLower(), likeSearch)
                );
            }

            #endregion

            #region Status Filter

            if (!string.IsNullOrWhiteSpace(status) &&
                int.TryParse(status, out int statusInt))
            {
                query = query.Where(x => x.f.Status == statusInt);
            }

            #endregion

            // ✅ Total Records
            var totalRecords = await query.CountAsync();

            // ✅ Data with Paging
            var data = await query
                .OrderByDescending(x => x.f.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LeadFollowupDto
                {
                    FollowUpID = x.f.FollowUpID,
                    LeadID = x.f.LeadID,
                    LeadNo = x.lead.LeadNo,

                    Notes = x.f.Notes,
                    NextFollowupDate = x.f.NextFollowupDate,

                    Status = x.f.Status,
                    StatusName = x.statusName != null
                        ? x.statusName.StatusName
                        : null,

                    FollowUpBy = x.f.FollowUpBy,
                    FollowUpByName = x.user != null
                        ? x.user.UserName
                        : null,

                    CreatedDate = x.f.CreatedDate,

                    ClientName = x.client.ContactPerson,
                    CompanyName = x.client.CompanyName,
                    Mobile = x.client.Mobile,
                    Email = x.client.Email
                })
                .ToListAsync();

            // ✅ Return PagedResult
            return new PagedResult<LeadFollowupDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }


        public async Task<(bool leadExists, List<LeadFollowupDto>? followups)> GetFollowupHistoryAsync(Guid leadId)
        {
                bool leadExists = await _context.Leads.AnyAsync(l => l.LeadID == leadId);
                if (!leadExists)
                    return (false, null);

                var followups = await (from f in _context.LeadFollowups
                                       join l in _context.Leads on f.LeadID equals l.LeadID
                                       join c in _context.Clients on l.ClientID equals c.ClientID
                                       join s in _context.LeadFollowupStatuses on f.Status equals s.LeadFollowupStatusID
                                       join u in _context.Users on f.FollowUpBy equals u.Id into users
                                       from cu in users.DefaultIfEmpty()
                                       where f.LeadID == leadId
                                       orderby f.CreatedDate descending
                                       select new LeadFollowupDto
                                       {
                                           FollowUpID = f.FollowUpID,
                                           LeadID = f.LeadID,
                                           LeadNo = l.LeadNo,
                                           Notes = f.Notes,
                                           NextFollowupDate = f.NextFollowupDate,
                                           Status = f.Status,
                                           StatusName = s.StatusName,
                                           FollowUpBy = f.FollowUpBy,
                                           FollowUpByName = cu != null ? cu.UserName : null,
                                           CreatedDate = f.CreatedDate,
                                           ClientName =c.ContactPerson,
                                           Mobile =c.Mobile,
                                           Email =c.Email,
                                           CompanyName =c.CompanyName,
                                       }).ToListAsync();

                return (true, followups);
        }
    }
}
