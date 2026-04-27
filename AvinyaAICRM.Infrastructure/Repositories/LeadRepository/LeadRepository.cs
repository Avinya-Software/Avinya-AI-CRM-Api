using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Domain.Enums.Clients;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Infrastructure.Service;
using AvinyaAICRM.Shared.Model;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace AvinyaAICRM.Infrastructure.Repositories.LeadRepository
{
    public class LeadRepository : ILeadRepository
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INumberGeneratorService _numberGeneratorService;

        public LeadRepository(AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
             INumberGeneratorService numberGeneratorService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _numberGeneratorService = numberGeneratorService;
        }

        public async Task<IEnumerable<LeadDropdown>> GetAllAsync(string? tenantId, string? role)
        {
            var query = _context.Leads.Where(l => !l.IsDeleted);

            if (role != "SuperAdmin")
            {
                query = query.Where(l => l.TenantId.ToString() == tenantId);
            }


            return await _context.Leads
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new LeadDropdown
                {
                    LeadID = c.LeadID,
                    LeadNo = c.LeadNo
                })
                .ToListAsync();
        }

        public async Task<LeadDto?> GetByIdAsync(Guid id, string? tenantId, string? role)
        {
            var query = _context.Leads.Where(l => l.LeadID == id && !l.IsDeleted);

            if (role != "SuperAdmin")
            {
                query = query.Where(l => l.TenantId.ToString() == tenantId);
            }

            var lead = await query.FirstOrDefaultAsync();

            if (lead == null)
                return null;

            var clients = await _context.Clients.ToListAsync();
            var users = await _context.Users
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var leadSources = await _context.leadSourceMasters.ToListAsync();
            var statuses = await _context.leadStatusMasters.ToListAsync();

            var latestFollowupDate = await GetLatestFollowupDateAsync(id);

            var client = clients.FirstOrDefault(c => c.ClientID == lead.ClientID);

            var createdByName = users.FirstOrDefault(u => u.Id == lead.CreatedBy)?.UserName;
            var assignedToName = users.FirstOrDefault(u => u.Id == lead.AssignedTo)?.UserName;

            // ✅ Direct Guid? == Guid? comparison
            string? sourceName = leadSources
                .FirstOrDefault(ls => ls.LeadSourceID == lead.LeadSourceID)
                ?.SourceName;


            string? statusName = statuses
                .FirstOrDefault(s => s.LeadStatusID == lead.LeadStatusID)
                ?.StatusName;

            var followups = await (
                from f in _context.LeadFollowups
                join u in _context.Users on f.FollowUpBy equals u.Id into fu
                from user in fu.DefaultIfEmpty()

                join fs in _context.LeadFollowupStatuses
                    on f.Status equals fs.LeadFollowupStatusID into fsj
                from fs in fsj.DefaultIfEmpty()

                where f.LeadID == id
                orderby f.CreatedDate descending

                select new LeadFollowupDetailsDto
                {
                    FollowUpID = f.FollowUpID,
                    LeadID = f.LeadID,
                    Notes = f.Notes,
                    NextFollowupDate = f.NextFollowupDate,
                    Status = f.Status,
                    StatusName = fs != null ? fs.StatusName : null,
                    FollowUpBy = f.FollowUpBy,
                    FollowUpByName = user != null ? user.UserName : null,
                    CreatedDate = f.CreatedDate,
                    UpdatedDate = f.UpdatedDate
                }
            ).ToListAsync();

            var latestFollowup = followups.FirstOrDefault();

            return new LeadDto
            {
                LeadID = lead.LeadID,
                LeadNo = lead.LeadNo,
                ClientID = lead.ClientID,

                ContactPerson = client?.ContactPerson ?? "",
                Mobile = client?.Mobile ?? "",
                Email = client?.Email ?? "",
                StateID = client?.StateID,
                CityID = client?.CityID,
                StateName = _context.States.Where(s=>s.StateID == client.StateID).Select(s=> s.StateName).FirstOrDefault() ?? "",
                CityName = _context.Cities.Where(s=>s.CityID == client.CityID).Select(c=> c.CityName).FirstOrDefault()??"",

                Notes = lead.Notes,
                Links = lead.Links,
                RequirementDetails = lead.RequirementDetails,
                LeadSourceID = lead.LeadSourceID,
                LeadSourceName = sourceName,
                OtherSources = lead.OtherSources,
                LeadStatusID = lead.LeadStatusID,
                StatusName = statusName,
                CreatedBy = lead.CreatedBy,
                CreatedbyName = createdByName,
                AssignedTo = lead.AssignedTo,
                AssignToName = assignedToName,
                CreatedDate = lead.CreatedDate,

                ClientType = client?.ClientType ?? 0,
                clientTypeName = client != null
                ? Enum.GetName(typeof(ClientTypeEnum), client.ClientType)
                : "",

                CompanyName = client?.CompanyName ?? "",
                GSTNo = client?.GSTNo ?? "",
                BillingAddress = client?.BillingAddress ?? "",

                Followups = followups,
                FollowupCount = followups.Count,

                LatestLeadFollowupId = latestFollowup?.FollowUpID,
                LatestFollowupStatus = latestFollowup?.StatusName,

                NextFollowupDate = latestFollowupDate
            };
        }


        public async Task<DateTime?> GetLatestFollowupDateAsync(Guid leadId)
        {
            return await _context.LeadFollowups
                .Where(f => f.LeadID == leadId)
                .OrderByDescending(f => f.CreatedDate)
                .Select(f => (DateTime?)f.NextFollowupDate)
                .FirstOrDefaultAsync();
        }

        public async Task<(bool IsValid, string? Message)> ValidateClientAsync(LeadRequestDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.GSTNo) && dto.GSTNo != "string")
            {
                bool gstExists = await _context.Clients
                    .AnyAsync(c => c.GSTNo == dto.GSTNo && !c.IsDeleted && c.ClientID != dto.ClientID);

                if (gstExists)
                    return (false, "GST Number already exists for another client.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != "string")
            {
                bool emailExists = await _context.Clients
                    .AnyAsync(c => c.Email == dto.Email && !c.IsDeleted && c.ClientID != dto.ClientID);

                if (emailExists)
                    return (false, "Email already exists for another client.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Mobile) && dto.Mobile != "string")
            {
                bool mobileExists = await _context.Clients
                    .AnyAsync(c => c.Mobile == dto.Mobile && !c.IsDeleted && c.ClientID != dto.ClientID);

                if (mobileExists)
                    return (false, "Mobile number already exists for another client.");
            }

            return (true, null);
        }

        public async Task<Lead> AddAsync(LeadRequestDto dto, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userData = await _context.Users.FindAsync(userId);

                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("Session expired. Please login again.");
                }

                Guid? finalClientId = dto.ClientID;

                
                // If no ClientID is passed → Create new Client
                if (dto.ClientID == null)
                {

                    var newClient = new Client
                    {
                        ClientID = Guid.NewGuid(),
                        CompanyName = dto.CompanyName ?? "",
                        ContactPerson = dto.ContactPerson ?? "",
                        Mobile = dto.Mobile,
                        Email = dto.Email ?? "",
                        GSTNo = dto.GSTNo ?? "",
                        BillingAddress = dto.BillingAddress ?? "",
                        StateID = dto.StateID,
                        CityID = dto.CityID,
                        ClientType = dto.ClientType ?? 0,
                        Status = true,
                        Notes = "",
                        CreatedBy = userId,
                        UpdatedAt = null,
                        IsDeleted = false,
                        TenantId = userData.TenantId
                    };

                    await _context.Clients.AddAsync(newClient);
                    await _context.SaveChangesAsync();

                    // Use new client ID in lead record
                    finalClientId = newClient.ClientID;
                }
                


                var newStatusId = await _context.leadStatusMasters
                   .Where(x => x.StatusName == "New")
                   .Select(x => x.LeadStatusID)


                   .FirstOrDefaultAsync();

                // ✅ Replace with this
                Guid leadStatusId = dto.LeadStatusID ?? newStatusId;

                

                var lead = new Lead
                {
                    LeadID = Guid.NewGuid(),
                    ClientID = finalClientId,
                    CreatedDate = dto.CreatedDate ?? DateTime.Now,
                    RequirementDetails = dto.RequirementDetails,
                    Notes = dto.Notes,
                    Links = dto.Links,
                    LeadSourceID = dto.LeadSourceID,
                    OtherSources = dto.OtherSources,
                    LeadStatusID = leadStatusId,
                    CreatedBy = userId,
                    AssignedTo = dto.AssignedTo,
                    TenantId = userData.TenantId
                };

                // Generate LeadNo
                //var lastLeadNo = await _context.Leads
                // .OrderByDescending(l => l.CreatedDate)
                // .Select(l => l.LeadNo)
                // .FirstOrDefaultAsync();

                //int nextNumber = 1;

                //if (!string.IsNullOrEmpty(lastLeadNo) && lastLeadNo.Contains("-"))
                //{
                //    var parts = lastLeadNo.Split("-");
                //    if (int.TryParse(parts.Last(), out int parsedNumber))
                //    {
                //        nextNumber = parsedNumber + 1;
                //    }
                //}

                lead.LeadNo = await _numberGeneratorService.GenerateNumberAsync("LeadNo", userData.TenantId.ToString());

                await _context.Leads.AddAsync(lead);
                await _context.SaveChangesAsync();

                // Auto create Follow-Up
                var follow = new LeadFollowups
                {
                    FollowUpID = Guid.NewGuid(),
                    LeadID = lead.LeadID,
                    Notes = "Add your Follow-Up notes.",
                    NextFollowupDate = dto.NextFollowupDate,
                    Status = 1, // Pending
                    FollowUpBy = lead.AssignedTo
                };

                await _context.LeadFollowups.AddAsync(follow);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return lead;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Lead?> UpdateAsync(LeadRequestDto dto, string? tenantId, string? role)
        {
            try
            {
                var query = _context.Leads.Where(l => l.LeadID == dto.LeadID && !l.IsDeleted);

                if (role != "SuperAdmin")
                {
                    query = query.Where(l => l.TenantId.ToString() == tenantId);
                }

                var existing = await query.FirstOrDefaultAsync();

                if (existing == null) return null;

                // Update lead
                if (dto.ClientID != Guid.Empty)
                {
                    bool clientExists = await _context.Leads.AnyAsync(c => c.ClientID == dto.ClientID);
                    if (clientExists)
                        existing.ClientID = dto.ClientID;
                }

                if (dto.CreatedDate.HasValue && dto.CreatedDate.Value != default)
                    existing.CreatedDate = dto.CreatedDate.Value;

                if (!string.IsNullOrWhiteSpace(dto.RequirementDetails) && dto.RequirementDetails != "string")
                    existing.RequirementDetails = dto.RequirementDetails;

                if (!string.IsNullOrWhiteSpace(dto.OtherSources) && dto.OtherSources != "string")
                    existing.OtherSources = dto.OtherSources;
                if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes != "string")
                    existing.Notes = dto.Notes;
                if (!string.IsNullOrWhiteSpace(dto.Links) && dto.Links != "string")
                    existing.Links = dto.Links;

            
                    existing.LeadSourceID = dto.LeadSourceID;
                    existing.LeadStatusID = dto.LeadStatusID;


                if (!string.IsNullOrWhiteSpace(dto.CreatedBy) && dto.CreatedBy != "string")
                {
                    var userExists = await _context.Users.AnyAsync(u => u.Id == dto.CreatedBy);
                    if (userExists)
                        existing.CreatedBy = dto.CreatedBy;
                }

                if (!string.IsNullOrWhiteSpace(dto.AssignedTo) && dto.AssignedTo != "string")
                {
                    var userExists = await _context.Users.AnyAsync(u => u.Id == dto.AssignedTo);
                    if (userExists)
                        existing.AssignedTo = dto.AssignedTo;
                }

                bool clientDataProvided =
                   !string.IsNullOrWhiteSpace(dto.CompanyName) ||
                   !string.IsNullOrWhiteSpace(dto.ContactPerson) ||
                   !string.IsNullOrWhiteSpace(dto.Email) ||
                   !string.IsNullOrWhiteSpace(dto.Mobile) ||
                   !string.IsNullOrWhiteSpace(dto.GSTNo) ||
                   !string.IsNullOrWhiteSpace(dto.BillingAddress) ||
                   dto.ClientType.HasValue;

                if (clientDataProvided)
                {
                    if (dto.ClientID == null || dto.ClientID == Guid.Empty)
                    {
                        // -------- CREATE NEW CLIENT ----------
                        var newClient = new Client
                        {
                            ClientID = Guid.NewGuid(),
                            CompanyName = dto.CompanyName ?? "",
                            ContactPerson = dto.ContactPerson ?? "",
                            Mobile = dto.Mobile,
                            Email = dto.Email ?? "",
                            GSTNo = dto.GSTNo ?? "",
                            BillingAddress = dto.BillingAddress ?? "",
                            StateID = dto.StateID,
                            CityID = dto.CityID,
                            ClientType = dto.ClientType ?? 0,
                            Status = true,
                            Notes = "",
                            CreatedBy = existing.CreatedBy,
                            CreatedDate = DateTime.Now,
                            UpdatedAt = null,
                            IsDeleted = false,
                            TenantId = Guid.Parse(tenantId)
                        };

                        await _context.Clients.AddAsync(newClient);
                        existing.ClientID = newClient.ClientID;
                    }
                    else
                    {
                        // -------- UPDATE EXISTING CLIENT ----------
                        var client = await _context.Clients
                            .FirstOrDefaultAsync(c => c.ClientID == dto.ClientID && !c.IsDeleted);

                        if (client != null)
                        {
                            if (!string.IsNullOrWhiteSpace(dto.ContactPerson) && dto.ContactPerson != "string")
                                client.ContactPerson = dto.ContactPerson;

                            if (!string.IsNullOrWhiteSpace(dto.Mobile) && dto.Mobile != "string")
                                client.Mobile = dto.Mobile;

                            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != "string")
                                client.Email = dto.Email;

                            if (dto.ClientType.HasValue && dto.ClientType.Value > 0)
                            {
                                client.ClientType = dto.ClientType.Value;
                            }

                            if (!string.IsNullOrWhiteSpace(dto.CompanyName) && dto.CompanyName != "string")
                                client.CompanyName = dto.CompanyName;

                            if (!string.IsNullOrWhiteSpace(dto.GSTNo) && dto.GSTNo != "string")
                                client.GSTNo = dto.GSTNo;

                            if (!string.IsNullOrWhiteSpace(dto.BillingAddress) && dto.BillingAddress != "string")
                                client.BillingAddress = dto.BillingAddress;

                            if (dto.StateID != null && dto.StateID!=0)
                                client.StateID = dto.StateID;

                            if (dto.CityID != null && dto.CityID !=0)
                                client.CityID = dto.CityID;

                        }
                        existing.ClientID = client.ClientID;
                    }
                }
                // Update Followup
                var latest = await _context.LeadFollowups
                    .Where(f => f.LeadID == existing.LeadID)
                    .OrderByDescending(f => f.CreatedDate)
                    .FirstOrDefaultAsync();

                if (latest != null)
                {
                    latest.NextFollowupDate = dto.NextFollowupDate;
                    latest.UpdatedDate = DateTime.Now;

                    if (!string.IsNullOrWhiteSpace(dto.AssignedTo))
                    {
                        bool userExists = await _context.Users.AnyAsync(u => u.Id == dto.AssignedTo);
                        if (userExists)
                            latest.FollowUpBy = dto.AssignedTo;
                    }
                }
                else
                {
                    var follow = new LeadFollowups
                    {
                        FollowUpID = Guid.NewGuid(),
                        LeadID = existing.LeadID,
                        Notes = "Initial follow-up created automatically.",
                        NextFollowupDate = dto.NextFollowupDate,
                        Status = 1,
                        FollowUpBy = dto.AssignedTo,
                        //CreatedDate = DateTime.Now,
                    };

                    await _context.LeadFollowups.AddAsync(follow);
                }


                await _context.SaveChangesAsync();
                return existing;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        public async Task<ResponseModel> UpdateLeadStatusAsync(Lead lead)
        {
            _context.Leads.Update(lead);
            await _context.SaveChangesAsync();
            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Status Updated Succesfully",
                Data = null
            };
        }

        public async Task<Lead?> GetLeadByIdAsync(Guid Id)
        {
            var existingLead = await _context.Leads.FindAsync(Id);
            return existingLead;
        }

        public async Task<bool> DeleteAsync(Guid id, string deletedBy, string? tenantId, string? role)
        {
            var query = _context.Leads.Where(l => l.LeadID == id && !l.IsDeleted);

            if (role != "SuperAdmin")
            {
                query = query.Where(l => l.TenantId.ToString() == tenantId);
            }

            var existing = await query.FirstOrDefaultAsync();

            if (existing == null) return false;

            existing.IsDeleted = true;
            existing.DeletedBy = deletedBy;
            existing.DeletedDate = DateTime.Now;

            _context.Leads.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<LeadDto>> GetFilteredAsync(
         string? search,
         string? statusFilter,
         DateTime? startDate,
         DateTime? endDate,
         int pageNumber,
         int pageSize,
         string userId,
         string? role)
        {
            try
            {
                var userData = await _context.Users.FindAsync(userId);
                var query = _context.Leads
                    .Where(l => !l.IsDeleted && l.TenantId == userData.TenantId)
                    .AsQueryable();

               
                //var role = user.FindFirst(ClaimTypes.Role)?.Value;

                var statuses = await _context.leadStatusMasters.ToListAsync();
                var sources = await _context.leadSourceMasters.ToListAsync();

                //// Employee restriction
                //if (role == "Employee" && !string.IsNullOrEmpty(userId))
                //    query = query.Where(l => l.CreatedBy == userId);

                #region SEARCH FILTER

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var likeSearch = $"%{search}%";

                    query =
                        from l in query
                        join c in _context.Clients
                            on l.ClientID equals c.ClientID into leadClient
                        from client in leadClient.DefaultIfEmpty()
                        where
                            EF.Functions.Like(client.ContactPerson, likeSearch) ||
                            EF.Functions.Like(client.Mobile, likeSearch) ||
                            EF.Functions.Like(client.Email, likeSearch) ||
                            EF.Functions.Like(l.LeadNo, likeSearch) ||
                            EF.Functions.Like(client.CompanyName, likeSearch) ||
                            EF.Functions.Like(client.GSTNo, likeSearch) ||
                            EF.Functions.Like(client.BillingAddress, likeSearch)
                        select l;
                }

                #endregion

                #region STATUS FILTER

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    bool isGuid = Guid.TryParse(statusFilter, out Guid statusGuid);

                    if (isGuid)
                    {
                        query = query.Where(l => l.LeadStatusID.ToString() == statusFilter);
                    }
                    else
                    {
                        var lower = statusFilter.ToLower();

                        var matchedStatusIds = statuses
                            .Where(s => s.StatusName.ToLower().Contains(lower))
                            .Select(s => s.LeadStatusID.ToString())
                            .ToList();

                        query = query.Where(l => matchedStatusIds.Contains(l.LeadStatusID.ToString()));
                    }
                }

                #endregion

                #region DATE FILTER

                DateTime? fromDate = startDate?.Date;
                DateTime? toDate = endDate?.Date;

                if (fromDate.HasValue && !toDate.HasValue)
                    toDate = DateTime.Today;

                if (toDate.HasValue)
                    toDate = toDate.Value.AddDays(1).AddTicks(-1);

                if (fromDate.HasValue && toDate.HasValue)
                    query = query.Where(l => l.CreatedDate >= fromDate && l.CreatedDate <= toDate);
                else if (toDate.HasValue)
                    query = query.Where(l => l.CreatedDate <= toDate);

                #endregion

                // ✅ Role Restriction
                if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) && 
                    !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) && 
                    !string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(l => l.CreatedBy == userId);
                }

                // ✅ Total count
                var totalRecords = await query.CountAsync();

             
                // ✅ Move .Where BEFORE .Skip/.Take (paging must come last)
                var leads = await query
                    .OrderByDescending(l => l.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                     .ToListAsync();

                var leadIds = leads.Select(l => l.LeadID).ToList();

                // Latest followups
                var followups = await _context.LeadFollowups
                    .Where(f => leadIds.Contains(f.LeadID))
                    .GroupBy(f => f.LeadID)
                    .Select(g => new
                    {
                        LeadID = g.Key,
                        LatestNextFollowupDate = g.OrderByDescending(x => x.CreatedDate)
                            .Select(x => (DateTime?)x.NextFollowupDate)
                            .FirstOrDefault(),

                        LatestFollowupStatusId = g.OrderByDescending(x => x.CreatedDate)
                            .Select(x => x.Status)
                            .FirstOrDefault(),

                        LatestLeadFollowupId = g.OrderByDescending(x => x.CreatedDate)
                            .Select(x => x.FollowUpID)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var clients = await _context.Clients.ToListAsync();
                var users = await _context.Users
                    .Select(u => new { u.Id, u.UserName })
                    .ToListAsync();

                var followupStatuses = await _context.LeadFollowupStatuses.ToListAsync();

                // ✅ DTO Mapping
                var result = leads.Select(l =>
                {
                    var followup = followups.FirstOrDefault(f => f.LeadID == l.LeadID);
                    var client = clients.FirstOrDefault(c => c.ClientID == l.ClientID);

                    var assignedToName =
                        users.FirstOrDefault(u => u.Id == l.AssignedTo)?.UserName;

                    var createdByName =
                        users.FirstOrDefault(u => u.Id == l.CreatedBy)?.UserName;

                    // ✅ Direct Guid? == Guid? comparison
                    string? statusName = statuses
                        .FirstOrDefault(s => s.LeadStatusID == l.LeadStatusID)
                        ?.StatusName;

                    // ✅ Direct Guid? == Guid? comparison
                    string? leadSourceName = sources
                        .FirstOrDefault(s => s.LeadSourceID == l.LeadSourceID)
                        ?.SourceName;

                    var followupStatusName =
                        followupStatuses
                            .FirstOrDefault(s =>
                                s.LeadFollowupStatusID == followup?.LatestFollowupStatusId)
                            ?.StatusName;

                    return new LeadDto
                    {
                        LeadID = l.LeadID,
                        LeadNo = l.LeadNo,
                        ClientID = l.ClientID,
                        ContactPerson = client?.ContactPerson ?? "",
                        Mobile = client?.Mobile ?? "",
                        Email = client?.Email ?? "",
                        CompanyName = client?.CompanyName ?? "",
                        BillingAddress = client?.BillingAddress ?? "",
                        GSTNo = client?.GSTNo ?? "",
                        ClientType = client.ClientType,
                        LeadStatusID = l.LeadStatusID,
                        StatusName = statusName,

                        LeadSourceID = l.LeadSourceID,
                        LeadSourceName = leadSourceName,

                        StateID = client.StateID ?? null,
                        CityID=client.CityID ?? null,

                        AssignedTo = l.AssignedTo,
                        AssignToName = assignedToName,

                        CreatedBy = l.CreatedBy,
                        CreatedbyName = createdByName,

                        CreatedDate = l.CreatedDate,

                        NextFollowupDate = followup?.LatestNextFollowupDate,
                        LatestLeadFollowupId = followup?.LatestLeadFollowupId,
                        LatestFollowupStatus = followupStatusName,
                        Links = l.Links,
                        RequirementDetails = l.RequirementDetails,
                        Notes = l.Notes,
                        CreateFollowup =
                            followupStatusName != null &&
                            followupStatusName.Equals("Completed",
                                StringComparison.OrdinalIgnoreCase)
                    };
                }).ToList();

                // ✅ FINAL RETURN (Same pattern as Users API)
                return new PagedResult<LeadDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    Data = result
                };
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<LeadSourceMaster>> GetAllLeadSourceAsync()
        {
            return await _context.leadSourceMasters
                .OrderBy(t => t.SortOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<LeadStatusMaster>> GetAllLeadStatusAsync()
        {
            return await _context.leadStatusMasters
                .OrderBy(t => t.SortOrder)
                .ToListAsync();
        }

        public async Task<List<LeadHistoryDto>> GetLeadHistoryAsync(Guid leadId)
        {
            try
            {
                var history = new List<LeadHistoryDto>();

                var lead = await _context.Leads
                .Where(l => l.LeadID == leadId && !l.IsDeleted)
                .FirstOrDefaultAsync();

                if (lead == null)
                    return new List<LeadHistoryDto>();

                var client = await _context.Clients
                    .Where(c => c.ClientID == lead.ClientID)
                    .FirstOrDefaultAsync();

                // ✅ Direct — unwrap Guid? with ?? fallback
                Guid statusId = lead.LeadStatusID ?? Guid.Empty;

                var status = await _context.leadStatusMasters
                    .Where(s => s.LeadStatusID == statusId)
                    .FirstOrDefaultAsync();

                DateTime ConvertUtcToLocal(DateTime utcDate) =>
                    TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                history.Add(new LeadHistoryDto
                {
                    EntityType = "Lead",
                    Action = "Lead Created",
                    Createddate = ConvertUtcToLocal(lead.CreatedDate),
                    ClientName = client?.ContactPerson,
                    CompanyName = client?.CompanyName,
                    Status = lead.LeadStatusID.ToString(),
                    StatusName = status?.StatusName
                });

                var followups = await _context.LeadFollowups
                    .Where(f => f.LeadID == leadId)
                    .OrderBy(f => f.CreatedDate)
                    .ToListAsync();

                foreach (var f in followups)
                {
                    history.Add(new LeadHistoryDto
                    {
                        EntityType = "Follow-Up",
                        Action = "Follow-Up Created",
                        Createddate = f.CreatedDate.HasValue ? ConvertUtcToLocal(f.CreatedDate.Value) : (DateTime?)null,
                        ClientName = client?.ContactPerson,
                        CompanyName = client?.CompanyName,
                        Status = f.Status.ToString(),
                        StatusName = status?.StatusName
                    });
                }

                var quotations = await _context.Quotations
                    .Where(x => x.LeadID == leadId)
                    .OrderBy(q => q.CreatedDate)
                    .ToListAsync();

                foreach (var q in quotations)
                {
                    history.Add(new LeadHistoryDto
                    {
                        EntityType = "Quotation",
                        Action = "Quotation Created",
                        Createddate = ConvertUtcToLocal(q.CreatedDate),
                        ClientName = client?.ContactPerson,
                        CompanyName = client?.CompanyName,
                        Status = q.QuotationStatusID.ToString(),
                        StatusName = status?.StatusName
                    });
                }

                var orders = await _context.Orders
                    .Where(x => x.Quotation.LeadID == leadId)
                    .OrderBy(x => x.CreatedDate)
                    .ToListAsync();

                foreach (var o in orders)
                {
                    history.Add(new LeadHistoryDto
                    {
                        EntityType = "Order",
                        Action = "Order Created",
                        Createddate = ConvertUtcToLocal(o.CreatedDate),
                        ClientName = client?.ContactPerson,
                        CompanyName = client?.CompanyName,
                        Status = o.Status.ToString(),
                        StatusName = status?.StatusName
                    });
                }

                history = history
                    .OrderByDescending(h => h.Createddate)
                    .ToList();

                return history;
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<List<LeadGroupDto>> GetAllLeadGrpByStatus()
        {
            // Get all leads
            var leads = await _context.Leads
                .Where(l => !l.IsDeleted)
                .ToListAsync();

            // Load lookups
            var statuses = await _context.leadStatusMasters.ToListAsync();
            var sources = await _context.leadSourceMasters.ToListAsync();

            var clients = await _context.Clients
                .Select(c => new
                {
                    c.ClientID,
                    c.ContactPerson,
                    c.ClientType,
                    c.CompanyName,
                    c.GSTNo,
                    c.BillingAddress,
                    c.Mobile,
                    c.Email,
                    c.StateID,
                    c.CityID
                })
                .ToListAsync();

            var users = await _context.Users
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var followupStatuses = await _context.LeadFollowupStatuses
                   .Select(s => new { s.LeadFollowupStatusID, s.StatusName })
                   .ToListAsync();

            // Get latest followup for each lead
            var leadIds = leads.Select(x => x.LeadID).ToList();

            var followups = await _context.LeadFollowups
                .Where(f => leadIds.Contains(f.LeadID))
                .GroupBy(f => f.LeadID)
                .Select(g => new
                {
                    LeadID = g.Key,
                    LatestNextFollowupDate = g.OrderByDescending(f => f.CreatedDate)
                                              .Select(f => (DateTime?)f.NextFollowupDate)
                                              .FirstOrDefault(),
                    LatestFollowupStatusId = g.OrderByDescending(f => f.CreatedDate)
                               .Select(f => f.Status)
                               .FirstOrDefault()
                })
                .ToListAsync();

            var leadDtos = leads.Select(l =>
            {
                var followup = followups.FirstOrDefault(f => f.LeadID == l.LeadID);
                var client = clients.FirstOrDefault(c => c.ClientID == l.ClientID);

                string? latestFollowupStatusName = null;

                if (followup?.LatestFollowupStatusId != null)
                {
                    latestFollowupStatusName = followupStatuses
                        .FirstOrDefault(s => s.LeadFollowupStatusID == followup.LatestFollowupStatusId)
                        ?.StatusName;
                }

                bool createFollowup =
                    latestFollowupStatusName != null &&
                    latestFollowupStatusName.Equals("Completed", StringComparison.OrdinalIgnoreCase);

                var createdByName = users.FirstOrDefault(u => u.Id == l.CreatedBy)?.UserName;
                var assignedToName = users.FirstOrDefault(u => u.Id == l.AssignedTo)?.UserName;

                // Resolve lead source name
                string? leadSourceName = sources
                    .FirstOrDefault(s => s.LeadSourceID == l.LeadSourceID)
                    ?.SourceName;

                // Resolve status name

                string? statusName = statuses
                    .FirstOrDefault(s => s.LeadStatusID == l.LeadStatusID)
                    ?.StatusName;

                return new LeadDto
                {
                    LeadID = l.LeadID,
                    LeadNo = l.LeadNo,
                    ClientID = l.ClientID,

                    ContactPerson = client?.ContactPerson,
                    Mobile = client?.Mobile,
                    Email = client?.Email,
                    StateID = client?.StateID,
                    CityID = client?.CityID,

                    RequirementDetails = l.RequirementDetails,

                    LeadSourceID = l.LeadSourceID,
                    LeadSourceName = leadSourceName,
                    OtherSources = l.OtherSources,

                    LeadStatusID = l.LeadStatusID,
                    StatusName = statusName,

                    AssignedTo = l.AssignedTo,
                    AssignToName = assignedToName,

                    CreatedBy = l.CreatedBy,
                    CreatedbyName = createdByName,

                    CreatedDate = l.CreatedDate,
                    ClientType = client?.ClientType ?? 0,
                    clientTypeName = client != null ? Enum.GetName(typeof(ClientTypeEnum), client.ClientType) : "Unknown",

                    CompanyName = client?.CompanyName,
                    GSTNo = client?.GSTNo,
                    BillingAddress = client?.BillingAddress,
                    Notes = l.Notes,
                    Links = l.Links,

                    NextFollowupDate = followup?.LatestNextFollowupDate,
                    CreateFollowup = createFollowup
                };
            }).ToList();

            var OrderByStatus = await _context.leadStatusMasters.ToListAsync();

            var groupedResult = OrderByStatus
                .Select(status => new LeadGroupDto
                {
                    StatusID = status.LeadStatusID.ToString(),
                    StatusName = status.StatusName,
                    Leads = leadDtos.Where(l => l.StatusName == status.StatusName).ToList()
                })
                .ToList();

            return groupedResult;

        }

        
    }
}
