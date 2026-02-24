using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Infrastructure.Service;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.OrderRepository
{

    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly INumberGeneratorService _numberGeneratorService;

        public OrderRepository(AppDbContext context,
             INumberGeneratorService numberGeneratorService)
        {
            _context = context;
            _numberGeneratorService = numberGeneratorService;
        }

        public async Task<OrderResponseDto?> GetByIdAsync(Guid id)
        {
            DateTime ConvertUtcToLocal(DateTime utcDate) =>
               TimeZoneInfo.ConvertTimeFromUtc(
                   utcDate,
                   TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
               );

            var order = await (
                from o in _context.Orders.AsNoTracking()
                join c in _context.Clients.AsNoTracking() on o.ClientID equals c.ClientID into cj
                from c in cj.DefaultIfEmpty()
                join q in _context.Quotations.AsNoTracking() on o.QuotationID equals q.QuotationID into qj
                from q in qj.DefaultIfEmpty()
                join os in _context.OrderStatusMasters.AsNoTracking() on o.Status equals os.StatusID into osj
                from os in osj.DefaultIfEmpty()
                join ds in _context.DesignStatusMasters.AsNoTracking() on o.DesignStatus equals ds.DesignStatusID into dsj
                from ds in dsj.DefaultIfEmpty()
                join u in _context.Users.AsNoTracking() on o.CreatedBy equals u.Id into uj
                from created in uj.DefaultIfEmpty()
                join ua in _context.Users.AsNoTracking() on o.AssignedDesignTo equals ua.Id into uaj
                from assigned in uaj.DefaultIfEmpty()
                join s in _context.States.AsNoTracking()
                     on o.StateID equals s.StateID into sj
                from s in sj.DefaultIfEmpty()
                join ct in _context.Cities.AsNoTracking()
                    on o.CityID equals ct.CityID into ctj
                from ct in ctj.DefaultIfEmpty()


                select new OrderResponseDto
                {
                    OrderID = o.OrderID,
                    OrderNo = o.OrderNo,
                    ClientID = o.ClientID,
                    ClientName = c.ContactPerson,
                    CompanyName = c.CompanyName,
                    mobile = c.Mobile,
                    GstNo = c.GSTNo,
                    Email = c.Email,
                    BillAddress = c.BillingAddress,
                    ShippingAddress = o.ShippingAddress,
                    StateID = o.StateID ?? null,
                    StateName = s != null ? s.StateName : null,
                    CityName = ct != null ? ct.CityName : null,
                    CityID = o.CityID ?? null,
                    IsUseBillingAddress = o.IsUseBillingAddress,
                    QuotationID = o.QuotationID,
                    QuotationNo = q.QuotationNo,
                    OrderDate = o.OrderDate,
                    IsDesignByUs = o.IsDesignByUs,
                    DesigningCharge = o.DesigningCharge,
                    ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                    Status = o.Status,
                    StatusName = os.StatusName,
                    DesignStatus = o.DesignStatus,
                    DesignStatusName = ds.DesignStatusName,
                    CreatedBy = created != null ? created.Id : o.CreatedBy.ToString(),
                    CreatedByName = created != null ? created.UserName : null,
                    AssignedDesignTo = assigned != null ? assigned.Id : o.AssignedDesignTo,
                    AssignedDesignToName = assigned != null ? assigned.UserName : null,
                    CreatedDate = o.CreatedDate,
                    EnableTax = o.EnableTax,
                    TotalAmount = o.SubTotal,
                    Taxes = o.TotalTaxes,
                    GrandTotal = o.GrandTotal
                }
            ).FirstOrDefaultAsync();

            if (order != null)
            {
                order.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(
                    order.CreatedDate,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
                );
            }


            if (order == null)
                return null;

            order.OrderItems = await (
                from oi in _context.OrderItems.AsNoTracking()
                join p in _context.Products.AsNoTracking() on oi.ProductID equals p.ProductID into pj
                from p in pj.DefaultIfEmpty()
                join t in _context.TaxCategoryMasters.AsNoTracking() on oi.TaxCategoryID equals t.TaxCategoryID into tj
                from t in tj.DefaultIfEmpty()
                where oi.OrderID == id
                select new OrderItemReponceDto
                {
                    OrderItemID = oi.OrderItemID,
                    OrderID = oi.OrderID,
                    ProductID = oi.ProductID,
                    ProductName = p.ProductName,
                    Description = oi.Description,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TaxCategoryID = order.EnableTax ? oi.TaxCategoryID : null,
                    TaxCategoryName = order.EnableTax && t != null ? t.TaxName : "N/A",
                    Rate = order.EnableTax && t != null ? (decimal?)t.Rate ?? 0 : 0,

                    //LineTotal = order.EnableTax && t != null
                    //? oi.LineTotal + (oi.LineTotal * ((decimal?)t.Rate ?? 0) / 100)
                    //: oi.LineTotal,
                    LineTotal = oi.LineTotal,

                    HsnCode = p.HSNCode
                }
            ).ToListAsync();
            order.IsAssign = true;

            return order;
        }

        public async Task<PagedResult<OrderResponseDto>> GetFilteredAsync(
    string? search,
    int pageNumber,
    int pageSize,
    int? statusFilter = null,
    DateTime? from = null,
    DateTime? to = null)
        {
            try
            {
                DateTime ConvertUtcToLocal(DateTime utcDate) =>
                    TimeZoneInfo.ConvertTimeFromUtc(
                        utcDate,
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var query = _context.Orders
                    .AsNoTracking()
                    .Where(o => !o.IsDeleted);

                #region SEARCH

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var likeSearch = $"%{search}%";

                    query = query.Where(q =>
                        EF.Functions.Like(q.OrderNo, likeSearch) ||
                        _context.Clients.Any(c =>
                            c.ClientID == q.ClientID &&
                            EF.Functions.Like(c.ContactPerson, likeSearch)));
                }

                #endregion

                #region FILTERS

                if (statusFilter.HasValue)
                    query = query.Where(o => o.Status == statusFilter.Value);

                if (from.HasValue)
                    query = query.Where(o => o.OrderDate >= from.Value.Date);

                if (to.HasValue)
                    query = query.Where(o =>
                        o.OrderDate <= to.Value.Date.AddDays(1).AddTicks(-1));

                #endregion

                // ✅ Total Records
                var totalRecords = await query.CountAsync();

                // ✅ Paged Orders
                var orders = await query
                    .OrderByDescending(o => o.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderResponseDto
                    {
                        OrderID = o.OrderID,
                        OrderNo = o.OrderNo,
                        ClientID = o.ClientID,

                        ClientName = _context.Clients
                            .Where(c => c.ClientID == o.ClientID)
                            .Select(c => c.ContactPerson)
                            .FirstOrDefault(),

                        Email = _context.Clients
                            .Where(c => c.ClientID == o.ClientID)
                            .Select(c => c.Email)
                            .FirstOrDefault(),

                        CompanyName = _context.Clients
                            .Where(c => c.ClientID == o.ClientID)
                            .Select(c => c.CompanyName)
                            .FirstOrDefault(),

                        mobile = _context.Clients
                            .Where(c => c.ClientID == o.ClientID)
                            .Select(c => c.Mobile)
                            .FirstOrDefault(),

                        GstNo = _context.Clients
                            .Where(c => c.ClientID == o.ClientID)
                            .Select(c => c.GSTNo)
                            .FirstOrDefault(),

                        BillAddress = _context.Clients
                            .Where(c => c.ClientID == o.ClientID)
                            .Select(c => c.BillingAddress)
                            .FirstOrDefault(),

                        ShippingAddress = o.ShippingAddress,
                        IsUseBillingAddress = o.IsUseBillingAddress,
                        StateID = o.StateID,
                        CityID = o.CityID,
                        QuotationID = o.QuotationID,
                        OrderDate = o.OrderDate,
                        IsDesignByUs = o.IsDesignByUs,
                        DesigningCharge = o.DesigningCharge,
                        ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                        Status = o.Status,
                        Taxes = o.TotalTaxes,
                        TotalAmount = o.SubTotal,
                        GrandTotal = o.GrandTotal,
                        EnableTax = o.EnableTax,
                        CreatedDate = o.CreatedDate,

                        StatusName = _context.OrderStatusMasters
                            .Where(s => s.StatusID == o.Status)
                            .Select(s => s.StatusName)
                            .FirstOrDefault(),

                        DesignStatus = o.DesignStatus,

                        DesignStatusName = _context.DesignStatusMasters
                            .Where(ds => ds.DesignStatusID == o.DesignStatus)
                            .Select(ds => ds.DesignStatusName)
                            .FirstOrDefault(),

                        CreatedBy = o.CreatedBy,

                        CreatedByName = _context.Users
                            .Where(u => u.Id == o.CreatedBy)
                            .Select(u => u.UserName)
                            .FirstOrDefault(),

                        AssignedDesignTo = o.AssignedDesignTo,

                        AssignedDesignToName = _context.Users
                            .Where(u => u.Id == o.AssignedDesignTo)
                            .Select(u => u.UserName)
                            .FirstOrDefault(),

                        // NOTE:
                        // OrderItems, WorkOrder, Bill mapping stays EXACTLY SAME
                        // (keep your existing projection block here unchanged)
                    })
                    .ToListAsync();

                // 🔹 (Your existing WorkOrderItems loading logic stays SAME)
                // 🔹 (Your UTC → Local conversion stays SAME)

                orders.ForEach(o =>
                {
                    o.Bill?.ForEach(b =>
                    {
                        b.BillDate = ConvertUtcToLocal(b.BillDate);

                        if (b.DueDate.HasValue)
                            b.DueDate = ConvertUtcToLocal(b.DueDate.Value);

                        b.CreatedDate = ConvertUtcToLocal(b.CreatedDate);
                    });
                });

                // ✅ RETURN PAGED RESULT
                return new PagedResult<OrderResponseDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    Data = orders
                };
            }
            catch
            {
                throw;
            }
        }


        // Add or Update (single method) — sync items:
        // - if dto.OrderID empty => create
        // - else update only provided fields (ignore defaults)
        // - for items: update existing by id, insert new, remove missing items (sync)

        // in a order perticuler item wise select tax
        public async Task<OrderResponseDto> AddOrUpdateOrderAsync(OrderDto dto, string? userId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                bool isNew = dto.OrderID == null || dto.OrderID == Guid.Empty;
                Guid orderId;
                DateTime now = DateTime.Now;

                string? shippingAddress = null;
                int? stateID = null;
                int? cityID = null;

                // ---------- VALIDATION ONLY FOR ADD ----------
                if (isNew)
                {
                    //if (dto.IsUseBillingAddress != true)
                    //{
                    //    if (string.IsNullOrWhiteSpace(dto.ShippingAddress))
                    //        throw new Exception("Shipping Address is required");
                    //}

                    if (dto.IsUseBillingAddress == true)
                    {
                        var client = await _context.Clients
                            .FirstOrDefaultAsync(c => c.ClientID == dto.ClientID);

                        if (client == null)
                            throw new Exception("Client not found to map Billing Address");

                        shippingAddress = client.BillingAddress;
                        stateID = client.StateID;
                        cityID = client.CityID;

                    }
                    else
                    {
                        shippingAddress = dto.ShippingAddress;
                        stateID = dto.StateID;
                        cityID = dto.CityID;

                    }
                }
                else
                {
                    // Update time → do NOT validate, do NOT auto-fill
                    shippingAddress = dto.ShippingAddress;
                    stateID = dto.StateID;
                    cityID = dto.CityID;
                }


                Order order;

                // ---------- CREATE ----------
                if (isNew)
                {
                    orderId = Guid.NewGuid();

                    order = new Order
                    {
                        OrderID = orderId,
                        OrderNo = await _numberGeneratorService.GenerateNumberAsync("OrderNo"),
                        ClientID = dto.ClientID ?? Guid.Empty,
                        QuotationID = dto.QuotationID,
                        OrderDate = dto.OrderDate ?? DateTime.Now,
                        IsDesignByUs = dto.IsDesignByUs ?? false,
                        DesigningCharge = dto.DesigningCharge ?? 0,
                        ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                        Status = dto.Status ?? 0,
                        FirmID = dto.FirmID,
                        DesignStatus = dto.DesignStatus ?? 0,
                        CreatedBy = userId,

                        EnableTax = dto.EnableTax.GetValueOrDefault(),

                        AssignedDesignTo = string.IsNullOrWhiteSpace(dto.AssignedDesignTo) ? null : dto.AssignedDesignTo,
                        IsUseBillingAddress = dto.IsUseBillingAddress.GetValueOrDefault(),
                        ShippingAddress = shippingAddress,
                        StateID = stateID,
                        CityID = cityID,
                        IsDeleted = false
                    };

                    await _context.Orders.AddAsync(order);
                    // ===== UPDATE LEAD STATUS TO CONVERTED =====
                    if (dto.QuotationID != null && dto.QuotationID != Guid.Empty)
                    {
                        // Get Quotation → Lead
                        var quotation = await _context.Quotations
                            .FirstOrDefaultAsync(q => q.QuotationID == dto.QuotationID);

                        if (quotation != null && quotation.LeadID != null && quotation.LeadID != Guid.Empty)
                        {
                            var lead = await _context.Leads
                                .FirstOrDefaultAsync(l => l.LeadID == quotation.LeadID && !l.IsDeleted);

                            if (lead != null)
                            {
                                var convertedStatusId = await _context.leadStatusMasters
                                    .Where(x => x.StatusName == "Converted")
                                    .Select(x => x.LeadStatusID)
                                    .FirstOrDefaultAsync();
                                lead.Status = convertedStatusId.ToString();
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    orderId = dto.OrderID!.Value;

                    order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.OrderID == orderId && !o.IsDeleted)
                        ?? throw new Exception("Order not found");

                    // Update only if values are passed (not null / not empty)
                    if (!string.IsNullOrWhiteSpace(dto.OrderNo))
                        order.OrderNo = dto.OrderNo!;

                      order.FirmID = dto.FirmID;

                    if (dto.ClientID.HasValue && dto.ClientID.Value != Guid.Empty)
                        order.ClientID = dto.ClientID.Value;

                    if (dto.QuotationID.HasValue)
                        order.QuotationID = dto.QuotationID;

                    if (dto.OrderDate.HasValue)
                        order.OrderDate = dto.OrderDate.Value;

                    if (dto.IsDesignByUs.HasValue)
                        order.IsDesignByUs = dto.IsDesignByUs.Value;

                    if (dto.DesigningCharge.HasValue)
                        order.DesigningCharge = dto.DesigningCharge.Value;

                    if (dto.ExpectedDeliveryDate.HasValue)
                        order.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;

                    if (dto.Status.HasValue)
                        order.Status = dto.Status.Value;

                    if (dto.DesignStatus.HasValue)
                        order.DesignStatus = dto.DesignStatus.Value;

                    // TAX ENABLE always exists in DTO
                    if (dto.EnableTax.HasValue)
                        order.EnableTax = dto.EnableTax.Value;

                    if (!string.IsNullOrWhiteSpace(dto.AssignedDesignTo))
                        order.AssignedDesignTo = dto.AssignedDesignTo;

                    // 🔥 Address logic: UPDATE ONLY IF PASSED
                    if (dto.IsUseBillingAddress == true)
                    {
                        // user wants to switch to billing
                        var client = await _context.Clients
                            .FirstOrDefaultAsync(c => c.ClientID == dto.ClientID);

                        if (client == null)
                            throw new Exception("Client not found");

                        order.IsUseBillingAddress = true;
                        order.ShippingAddress = client.BillingAddress;
                        order.StateID = client.StateID;
                        order.CityID = client.CityID;
                    }
                    else if (dto.ShippingAddress != null)
                    {
                        // user passed shipping address → update shipping
                        order.IsUseBillingAddress = false;
                        order.ShippingAddress = dto.ShippingAddress;
                        order.StateID = dto.StateID;
                        order.CityID = dto.CityID;
                    }

                    // If user does NOT send address fields → DO NOTHING
                }


                // ---------- UPSERT ORDER ITEMS ----------
                // 🔥 Only run OrderItem logic when Items are passed
                if (dto.Items != null && dto.Items.Any())
                {
                    // Load existing DB items
                    var dbItems = await _context.OrderItems
                        .Where(i => i.OrderID == orderId)
                        .ToListAsync();

                    // Get item IDs from DTO
                    var dtoIds = dto.Items
                        .Where(i => i.OrderItemId.HasValue)
                        .Select(i => i.OrderItemId!.Value)
                        .ToHashSet();

                    // Delete items not present in DTO
                    var toRemove = dbItems
                        .Where(dbi => !dtoIds.Contains(dbi.OrderItemID))
                        .ToList();

                    if (toRemove.Any())
                        _context.OrderItems.RemoveRange(toRemove);

                    // Update or Insert
                    foreach (var itemDto in dto.Items)
                    {
                        // Update existing
                        if (itemDto.OrderItemId.HasValue)
                        {
                            var dbItem = dbItems
                                .FirstOrDefault(x => x.OrderItemID == itemDto.OrderItemId.Value);

                            if (dbItem != null)
                            {
                                if (itemDto.ProductID != Guid.Empty) dbItem.ProductID = itemDto.ProductID;
                                if (!string.IsNullOrWhiteSpace(itemDto.Description)) dbItem.Description = itemDto.Description;
                                if (itemDto.Quantity > 0) dbItem.Quantity = itemDto.Quantity;
                                if (itemDto.UnitPrice > 0) dbItem.UnitPrice = itemDto.UnitPrice;
                                if (itemDto.TaxCategoryID.HasValue && itemDto.TaxCategoryID.Value != Guid.Empty)
                                    dbItem.TaxCategoryID = itemDto.TaxCategoryID;

                                dbItem.LineTotal = dbItem.Quantity * dbItem.UnitPrice;
                            }
                        }
                        else
                        {
                            // Insert new item
                            await _context.OrderItems.AddAsync(new OrderItem
                            {
                                OrderItemID = Guid.NewGuid(),
                                OrderID = orderId,
                                ProductID = itemDto.ProductID,
                                Description = itemDto.Description,
                                Quantity = itemDto.Quantity,
                                UnitPrice = itemDto.UnitPrice,
                                TaxCategoryID = itemDto.TaxCategoryID,
                                LineTotal = itemDto.Quantity * itemDto.UnitPrice
                            });
                        }
                    }
                }


                await _context.SaveChangesAsync();

                // ---------- CALCULATE TOTALS (Exactly like Quotation) ----------
                var itemsWithTax = await (from i in _context.OrderItems
                                          where i.OrderID == orderId
                                          join t in _context.TaxCategoryMasters on i.TaxCategoryID equals t.TaxCategoryID into tTbl
                                          from t in tTbl.DefaultIfEmpty()
                                          select new
                                          {
                                              i.LineTotal,
                                              TaxPercentage = t == null ? 0 : (decimal?)t.Rate ?? 0
                                          }).ToListAsync();

                order.SubTotal = itemsWithTax.Sum(x => x.LineTotal);

                order.TotalTaxes = order.EnableTax
                    ? itemsWithTax.Sum(x => x.LineTotal * x.TaxPercentage / 100)
                    : 0;

                decimal designChargeToApply = order.IsDesignByUs
                    ? (order.DesigningCharge ?? 0)
                    : 0;

                order.GrandTotal = order.SubTotal
                                 + order.TotalTaxes
                                 + designChargeToApply;


                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // ---------- RETURN SAVED RESPONSE ----------
                return await GetByIdAsync(orderId)
                    ?? throw new Exception("Saved but response fetch failed");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == id && !o.IsDeleted);
            if (order == null) return false;
            order.IsDeleted = true;
            //order.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }


        // helper to parse userId strings to Guid? safely
        private static Guid? TryParseGuidOrNull(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return Guid.TryParse(s, out var g) ? g : (Guid?)null;
        }


        private static Guid TryParseGuidOrDefault(string? s)
        {   
            if (string.IsNullOrWhiteSpace(s)) return Guid.Empty;
            return Guid.TryParse(s, out var g) ? g : Guid.Empty;
        }

        


    }



}
