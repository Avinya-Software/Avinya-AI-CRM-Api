using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Domain.Entities.Quotations;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Infrastructure.Service;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AvinyaAICRM.Infrastructure.Repositories.QuotationRepository
{
    public class QuotationRepository : IQuotationRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QuotationRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INumberGeneratorService _numberGeneratorService;

        public QuotationRepository(AppDbContext context,
            ILogger<QuotationRepository> logger,
            IHttpContextAccessor httpContextAccessor,
            INumberGeneratorService numberGeneratorService)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _numberGeneratorService = numberGeneratorService; ;

        }

        public async Task<(QuotationResponseDto Quotation, bool IsNew)> PostOrPutAsync(QuotationRequestDto dto)
        {
            try
            {
                bool isNew = false;
                var userId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("Session expired. Please login again.");
                }
                Quotation quotation;

                var sentStatusId = await _context.QuotationStatusMaster
                    .Where(x => x.StatusName == "Sent")
                    .Select(x => x.QuotationStatusID)
                    .FirstOrDefaultAsync();

                bool enableTax = dto.EnableTax ?? false;

                if (dto.QuotationID == null || dto.QuotationID == Guid.Empty)
                {
                    isNew = true;
                    quotation = new Quotation
                    {
                        QuotationID = Guid.NewGuid(),
                        QuotationNo = await _numberGeneratorService.GenerateNumberAsync("QuotationNo"),
                        ClientID = dto.ClientID,
                        LeadID = dto.LeadID,
                        FirmID= dto.FirmID,
                        QuotationDate = dto.QuotationDate,
                        ValidTill = dto.ValidTill ?? dto.QuotationDate.AddDays(15),
                         Status = dto.Status != null && dto.Status != Guid.Empty
                         ? dto.Status.Value
                         : sentStatusId,
                        TermsAndConditions = dto.TermsAndConditions,
                        EnableTax = enableTax,
                        RejectedNotes = dto.RejectedNotes,
                        CreatedBy = userId
                    };
                    await _context.Quotations.AddAsync(quotation);

                    var quotationSentStatusId = await _context.leadStatusMasters
                        .Where(x => x.StatusName == "Quotation Sent")
                        .Select(x => x.LeadStatusID)
                        .FirstOrDefaultAsync();

                    var lead = await _context.Leads
                        .FirstOrDefaultAsync(l => l.LeadID == dto.LeadID && !l.IsDeleted);

                    if (lead != null)
                    {
                        lead.Status = quotationSentStatusId.ToString();

                    }
                }
                else
                {
                    quotation = await _context.Quotations
                        .FirstOrDefaultAsync(q => q.QuotationID == dto.QuotationID && !q.IsDeleted);

                    if (dto.QuotationDate != default(DateTime))
                        quotation.QuotationDate = dto.QuotationDate;

                    quotation.ValidTill = dto.ValidTill ?? quotation.QuotationDate.AddDays(15);

                    quotation.EnableTax = enableTax;

                    if (dto.Status != null && dto.Status != Guid.Empty)
                    {
                        if (dto.Status != null && dto.Status != Guid.Empty)
                            quotation.Status = dto.Status.Value;

                        // If rejected, mark lead as Lost
                        var statusName = await _context.QuotationStatusMaster
                            .Where(x => x.QuotationStatusID == dto.Status)
                            .Select(x => x.StatusName)
                            .FirstOrDefaultAsync();

                        if (statusName == "Rejected")
                        {
                            var lead = await _context.Leads
                                .FirstOrDefaultAsync(l => l.LeadID == dto.LeadID && !l.IsDeleted);

                            if (lead != null)
                            {
                                var lostStatusId = await _context.leadStatusMasters
                                    .Where(x => x.StatusName == "Lost")
                                    .Select(x => x.LeadStatusID)
                                    .FirstOrDefaultAsync();

                                lead.Status = lostStatusId.ToString();
                            }
                        }
                    }

                    quotation.FirmID = dto.FirmID;

                    if (dto.Status != null && dto.Status != Guid.Empty)
                        quotation.Status = dto.Status.Value;

                    if (!string.IsNullOrWhiteSpace(dto.TermsAndConditions))
                        quotation.TermsAndConditions = dto.TermsAndConditions;

                    if (!string.IsNullOrWhiteSpace(dto.RejectedNotes))
                        quotation.RejectedNotes = dto.RejectedNotes;
                }
                await _context.SaveChangesAsync();

                // ---------- ITEMS PROCESS ----------
                if (dto.Items != null && dto.Items.Any())
                {
                    var existingItems = await _context.QuotationItems
                    .Where(i => i.QuotationID == quotation.QuotationID)
                    .ToListAsync();

                var reqItemsIds = dto.Items.Where(x => x.QuotationItemID != null)
                                           .Select(x => x.QuotationItemID).ToList();

                // DELETE removed items
                var toDelete = existingItems.Where(e => !reqItemsIds.Contains(e.QuotationItemID)).ToList();
                _context.QuotationItems.RemoveRange(toDelete);

                // UPSERT items
                foreach (var item in dto.Items)

                {
                    if (item.QuotationItemID != null)
                    {
                        var ex = existingItems.FirstOrDefault(x => x.QuotationItemID == item.QuotationItemID);
                        if (ex != null)
                        {
                            if (item.ProductID != Guid.Empty)
                                ex.ProductID = item.ProductID;

                            if (!string.IsNullOrWhiteSpace(item.Description))
                                ex.Description = item.Description;

                            if (item.Quantity > 0)
                                ex.Quantity = item.Quantity;

                            if (item.UnitPrice > 0)
                                ex.UnitPrice = item.UnitPrice;

                            if (item.TaxCategoryID != null && item.TaxCategoryID != Guid.Empty)
                                ex.TaxCategoryID = item.TaxCategoryID;

                            ex.LineTotal = ex.Quantity * ex.UnitPrice;
                        }
                    }
                    else // new add
                    {
                        _context.QuotationItems.Add(new QuotationItem
                        {
                            QuotationItemID = Guid.NewGuid(),
                            QuotationID = quotation.QuotationID,
                            ProductID = item.ProductID,
                            Description = item.Description,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TaxCategoryID = item.TaxCategoryID,
                            LineTotal = item.Quantity * item.UnitPrice
                        });
                    }
                }
                    await _context.SaveChangesAsync();
                }


                // ---------- CALCULATE TOTALS ----------
                var itemsWithTax = await (from i in _context.QuotationItems
                                          where i.QuotationID == quotation.QuotationID
                                          join t in _context.TaxCategoryMasters on i.TaxCategoryID equals t.TaxCategoryID into tTbl
                                          from t in tTbl.DefaultIfEmpty()
                                          select new
                                          {
                                              i.LineTotal,
                                              TaxPercentage = t != null ? t.Rate : 0
                                          }).ToListAsync();

                // Total of all line totals
                quotation.TotalAmount = itemsWithTax.Sum(x => x.LineTotal);

                // Total taxes from all items
                quotation.Taxes = quotation.EnableTax
             ? itemsWithTax.Sum(x => x.LineTotal * x.TaxPercentage / 100)
             : 0;

                // Grand total = TotalAmount + Taxes
                quotation.GrandTotal = quotation.TotalAmount + quotation.Taxes;

                // ---------- SAVE ALL CHANGES ONCE ----------
                await _context.SaveChangesAsync();

                // ---------- LOAD QUOTATION RESPONSE ----------
                var q = await (from qt in _context.Quotations
                               where qt.QuotationID == quotation.QuotationID && !qt.IsDeleted
                               join c in _context.Clients on qt.ClientID equals c.ClientID into cTbl
                               from c in cTbl.DefaultIfEmpty()
                               join l in _context.Leads on qt.LeadID equals l.LeadID into lTbl
                               from l in lTbl.DefaultIfEmpty()
                               join s in _context.QuotationStatusMaster on qt.Status equals s.QuotationStatusID into sTbl
                               from s in sTbl.DefaultIfEmpty()
                               join u in _context.Users on qt.CreatedBy equals u.Id into uTbl
                               from u in uTbl.DefaultIfEmpty()

                               select new QuotationResponseDto
                               {
                                   QuotationID = qt.QuotationID,
                                   QuotationNo = qt.QuotationNo,
                                   ClientID = qt.ClientID,
                                   CompanyName = c.CompanyName,
                                   Email = c.Email,
                                   Mobile = c.Mobile,
                                   ClientName = c.ContactPerson,
                                   LeadID = qt.LeadID,
                                   LeadNo = l.LeadNo,
                                   QuotationDate = qt.QuotationDate,
                                   ValidTill = qt.ValidTill,
                                   Status = qt.Status,
                                   EnableTax = qt.EnableTax,
                                   StatusName = s.StatusName,
                                   TermsAndConditions = qt.TermsAndConditions,
                                   RejectedNotes = qt.RejectedNotes,
                                   TotalAmount = qt.TotalAmount,
                                   Taxes = qt.Taxes,
                                   GrandTotal = qt.GrandTotal,
                                   CreatedBy = qt.CreatedBy,
                                   CreatedByName = u.UserName,
                               }).FirstOrDefaultAsync();

                q.Items = await (from i in _context.QuotationItems
                                 where i.QuotationID == quotation.QuotationID
                                 join p in _context.Products on i.ProductID equals p.ProductID into pTbl
                                 from p in pTbl.DefaultIfEmpty()
                                 join t in _context.TaxCategoryMasters on i.TaxCategoryID equals t.TaxCategoryID into tTbl
                                 from t in tTbl.DefaultIfEmpty()
                                 select new QuotationItemResponseDto
                                 {
                                     QuotationItemID = i.QuotationItemID,
                                     QuotationID = i.QuotationID,
                                     ProductID = i.ProductID,
                                     ProductName = p.ProductName,
                                     HsnCode = p.HSNCode,
                                     Description = i.Description,
                                     Quantity = i.Quantity,
                                     UnitPrice = i.UnitPrice,
                                     // ✅ LineTotal logic
                                     LineTotal = q.EnableTax
                                ? i.LineTotal + (
                                    _context.TaxCategoryMasters
                                        .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                        .Select(t => t.Rate)
                                        .FirstOrDefault()
                                  )
                                : i.LineTotal,

                                     // ✅ Tax logic
                                     TaxCategoryID = q.EnableTax ? i.TaxCategoryID : null,

                                     TaxCategoryName = q.EnableTax
                                ? _context.TaxCategoryMasters
                                    .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                    .Select(t => t.TaxName)
                                    .FirstOrDefault()
                                : "N/A",

                                     Rate = q.EnableTax
                                ? (
                                    _context.TaxCategoryMasters
                                        .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                        .Select(t => t.Rate)
                                        .FirstOrDefault()
                                  )
                                : 0
                                 }).ToListAsync();

                return (q, isNew);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public async Task<IEnumerable<QuotationDropdown>> GetAllAsync()
        {
            return await _context.QuotationStatusMaster
                .Where(c => c.IsActive == true)
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new QuotationDropdown
                {
                    QuotationStatusID = c.QuotationStatusID,
                    StatusName = c.StatusName
                })
                .ToListAsync();
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var quotation = await _context.Quotations.FirstOrDefaultAsync(q => q.QuotationID == id);
            if (quotation == null) return false;

            quotation.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QuotationResponseDto?> GetByIdAsync(Guid id)
        {

            var quotation = await (
                                    from q in _context.Quotations.AsNoTracking()
                                    where q.QuotationID == id && !q.IsDeleted

                                    let client = _context.Clients
                                        .AsNoTracking()
                                        .FirstOrDefault(c => c.ClientID == q.ClientID)

                                    let lead = _context.Leads
                                        .AsNoTracking()
                                        .FirstOrDefault(l => l.LeadID == q.LeadID)

                                    let status = _context.QuotationStatusMaster
                                        .AsNoTracking()
                                        .FirstOrDefault(s => s.QuotationStatusID == q.Status)

                                    let createdByUser = _context.Users
                                        .AsNoTracking()
                                        .FirstOrDefault(u => u.Id == q.CreatedBy)

                                    select new QuotationResponseDto
                                    {
                                        QuotationID = q.QuotationID,
                                        QuotationNo = q.QuotationNo,

                                        ClientID = q.ClientID,
                                        ClientName = client.ContactPerson,
                                        CompanyName = client.CompanyName,
                                        Email = client.Email,
                                        Mobile = client.Mobile,
                                        GstNo = client.GSTNo,
                                        BillAddress = client.BillingAddress,

                                        LeadID = q.LeadID,
                                        LeadNo = lead.LeadNo,

                                        EnableTax = q.EnableTax,
                                        Status = q.Status,
                                        StatusName = status.StatusName,

                                        QuotationDate = q.QuotationDate,
                                        ValidTill = q.ValidTill,
                                        TermsAndConditions = q.TermsAndConditions,
                                        RejectedNotes = q.RejectedNotes,

                                        TotalAmount = q.TotalAmount,
                                        Taxes = q.Taxes,
                                        GrandTotal = q.GrandTotal,

                                        CreatedBy = q.CreatedBy,
                                        CreatedByName = createdByUser.UserName,

                                        Items = (
                                            from i in _context.QuotationItems
                                            where i.QuotationID == q.QuotationID
                                            select new QuotationItemResponseDto
                                            {
                                                QuotationItemID = i.QuotationItemID,
                                                QuotationID = i.QuotationID,
                                                ProductID = i.ProductID,

                                                ProductName = _context.Products
                                                    .Where(p => p.ProductID == i.ProductID)
                                                    .Select(p => p.ProductName)
                                                    .FirstOrDefault(),

                                                HsnCode = _context.Products
                                                    .Where(p => p.ProductID == i.ProductID)
                                                    .Select(p => p.HSNCode)
                                                    .FirstOrDefault(),

                                                Description = i.Description,

                                                UnitName = (
                                                    from p in _context.Products
                                                    join u in _context.UnitTypeMasters
                                                        on p.UnitType equals u.UnitTypeID.ToString()
                                                    where p.ProductID == i.ProductID
                                                    select u.UnitName
                                                ).FirstOrDefault(),

                                                Quantity = i.Quantity,
                                                UnitPrice = i.UnitPrice,

                                                LineTotal =  i.LineTotal,

                                                TaxCategoryID = q.EnableTax ? i.TaxCategoryID : null,

                                                TaxCategoryName = q.EnableTax
                                                    ? _context.TaxCategoryMasters
                                                        .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                                        .Select(t => t.TaxName)
                                                        .FirstOrDefault()
                                                    : "N/A",

                                                Rate = q.EnableTax
                                                    ? _context.TaxCategoryMasters
                                                        .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                                        .Select(t => t.Rate)
                                                        .FirstOrDefault()
                                                    : 0
                                            }
                                        ).ToList()
                                    }
                                ).FirstOrDefaultAsync();

            return quotation;
        }


        public async Task<PagedResult<QuotationResponseDto>> FilterAsync(
     string? search,
     string? statusFilter,
     DateTime? startDate,
     DateTime? endDate,
     int pageNumber,
     int pageSize)
        {
            var query = _context.Quotations
                .AsNoTracking()
                .Where(q => !q.IsDeleted);

            #region SEARCH

            if (!string.IsNullOrWhiteSpace(search))
            {
                var likeSearch = $"%{search}%";

                query = query.Where(q =>
                    EF.Functions.Like(q.QuotationNo, likeSearch) ||
                    _context.Clients.Any(c =>
                        c.ClientID == q.ClientID &&
                        EF.Functions.Like(c.ContactPerson, likeSearch)));
            }

            #endregion

            #region STATUS FILTER

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                bool isGuid = Guid.TryParse(statusFilter, out Guid statusId);

                if (isGuid)
                {
                    query = query.Where(q => q.Status == statusId);
                }
                else
                {
                    var lower = statusFilter.ToLower();

                    query = query.Where(q =>
                        _context.QuotationStatusMaster
                            .Where(s => s.QuotationStatusID == q.Status)
                            .Select(s => s.StatusName.ToLower())
                            .FirstOrDefault()
                            .Contains(lower));
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
                query = query.Where(q =>
                    q.QuotationDate >= fromDate &&
                    q.QuotationDate <= toDate);
            else if (toDate.HasValue)
                query = query.Where(q => q.QuotationDate <= toDate);

            #endregion

            // ✅ TOTAL COUNT
            var totalRecords = await query.CountAsync();

            // Product + Unit lookup
            var productUnitQuery =
                from p in _context.Products
                join u in _context.UnitTypeMasters
                    on p.UnitType equals u.UnitTypeID.ToString() into pu
                from u in pu.DefaultIfEmpty()
                select new
                {
                    p.ProductID,
                    p.ProductName,
                    p.HSNCode,
                    UnitName = u != null ? u.UnitName : null
                };

            // ✅ STEP 1 — DB FETCH
            var rawData = await query
                .OrderByDescending(q => q.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new
                {
                    Quotation = q,

                    Client = _context.Clients
                        .FirstOrDefault(c => c.ClientID == q.ClientID),

                    LeadNo = _context.Leads
                        .Where(l => l.LeadID == q.LeadID)
                        .Select(l => l.LeadNo)
                        .FirstOrDefault(),

                    StatusName = _context.QuotationStatusMaster
                        .Where(s => s.QuotationStatusID == q.Status)
                        .Select(s => s.StatusName)
                        .FirstOrDefault(),

                    CreatedByName = _context.Users
                        .Where(u => u.Id == q.CreatedBy)
                        .Select(u => u.UserName)
                        .FirstOrDefault(),

                    Items =
                        (from i in _context.QuotationItems
                         where i.QuotationID == q.QuotationID
                         join pu in productUnitQuery
                             on i.ProductID equals pu.ProductID
                         select new QuotationItemResponseDto
                         {
                             QuotationItemID = i.QuotationItemID,
                             QuotationID = i.QuotationID,
                             ProductID = i.ProductID,
                             ProductName = pu.ProductName,
                             HsnCode = pu.HSNCode,
                             UnitName = pu.UnitName,
                             Description = i.Description,
                             Quantity = i.Quantity,
                             UnitPrice = i.UnitPrice,
                             LineTotal = i.LineTotal,
                             TaxCategoryID = q.EnableTax ? i.TaxCategoryID : null,
                             TaxCategoryName = q.EnableTax
                                ? _context.TaxCategoryMasters
                                    .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                    .Select(t => t.TaxName)
                                    .FirstOrDefault()
                                : "N/A",
                             Rate = q.EnableTax
                                ? _context.TaxCategoryMasters
                                    .Where(t => t.TaxCategoryID == i.TaxCategoryID)
                                    .Select(t => t.Rate)
                                    .FirstOrDefault()
                                : 0
                         }).ToList()
                })
                .ToListAsync();

            // ✅ STEP 2 — MEMORY MAPPING
            var data = rawData.Select(x => new QuotationResponseDto
            {
                QuotationID = x.Quotation.QuotationID,
                QuotationNo = x.Quotation.QuotationNo,
                ClientID = x.Quotation.ClientID,
                ClientName = x.Client?.ContactPerson,
                CompanyName = x.Client?.CompanyName,
                Email = x.Client?.Email,
                Mobile = x.Client?.Mobile,
                BillAddress = x.Client?.BillingAddress,
                GstNo = x.Client?.GSTNo,

                LeadID = x.Quotation.LeadID,
                LeadNo = x.LeadNo,

                QuotationDate = x.Quotation.QuotationDate,
                ValidTill = x.Quotation.ValidTill,
                EnableTax = x.Quotation.EnableTax,

                Status = x.Quotation.Status,
                StatusName = x.StatusName,

                TermsAndConditions = x.Quotation.TermsAndConditions,
                RejectedNotes = x.Quotation.RejectedNotes,
                TotalAmount = x.Quotation.TotalAmount,
                Taxes = x.Quotation.Taxes,
                GrandTotal = x.Quotation.GrandTotal,

                CreatedBy = x.Quotation.CreatedBy,
                CreatedByName = x.CreatedByName,

                Items = x.Items
            }).ToList();

            // ✅ RETURN PAGED RESULT
            return new PagedResult<QuotationResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }



    }
}
