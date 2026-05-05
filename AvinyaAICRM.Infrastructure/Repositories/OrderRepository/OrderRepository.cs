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

        public async Task<OrderResponseDto?> GetByIdAsync(Guid id, string tenantId)
        {
            var order = await (
                from o in _context.Orders.AsNoTracking()
                join c in _context.Clients.AsNoTracking() on o.ClientID equals c.ClientID into cj
                from c in cj.DefaultIfEmpty()
                join q in _context.Quotations.AsNoTracking() on o.QuotationID equals q.QuotationID into qj
                from q in qj.DefaultIfEmpty()
                join os in _context.OrderStatusMasters.AsNoTracking() on o.Status equals os.StatusID into osj
                from os in osj.DefaultIfEmpty()
                join ds in _context.DesignStatusMasters.AsNoTracking() on o.DesignStatusID equals ds.DesignStatusID into dsj
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
                join t in _context.Tenants.AsNoTracking() on o.TenantId equals t.TenantId into tj
                from t in tj.DefaultIfEmpty()
                where o.TenantId.ToString() == tenantId && o.OrderID == id

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
                    DesignStatus = o.DesignStatusID,
                    DesignStatusName = ds.DesignStatusName,
                    CreatedBy =  o.CreatedBy,
                    CreatedByName = created != null ? created.UserName : null,
                    AssignedDesignTo = assigned != null ? assigned.Id : o.AssignedDesignTo,
                    AssignedDesignToName = assigned != null ? assigned.UserName : null,
                    CreatedDate = o.CreatedDate,
                    EnableTax = o.EnableTax,
                    TotalAmount = o.SubTotal,
                    Taxes = o.TotalTaxes,
                    GrandTotal = o.GrandTotal,

                    // Map Firm details from Tenant
                    FirmName = t != null ? t.CompanyName : "AVINYA AI",
                    FirmAddress = t != null ? t.Address : "-",
                    FirmMobile =  t.CompanyPhone ?? "-" ,
                    FirmGSTNo = "-" // Tenant entity might not have GSTNo yet
                }
            ).FirstOrDefaultAsync();

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

            var activeBanks = await _context.BankDetails.AsNoTracking()
                .Where(b => b.TenantId == tenantId && b.IsActive)
                .Take(2)
                .ToListAsync();

            if (activeBanks.Count > 0)
            {
                order.Bank1 = new BankDetailsDto
                {
                    BankAccountId = activeBanks[0].BankAccountId,
                    BankName = activeBanks[0].BankName,
                    AccountHolderName = activeBanks[0].AccountHolderName,
                    AccountNumber = activeBanks[0].AccountNumber,
                    IFSCCode = activeBanks[0].IFSCCode,
                    BranchName = activeBanks[0].BranchName,
                    IsActive = activeBanks[0].IsActive
                };
            }
            if (activeBanks.Count > 1)
            {
                order.Bank2 = new BankDetailsDto
                {
                    BankAccountId = activeBanks[1].BankAccountId,
                    BankName = activeBanks[1].BankName,
                    AccountHolderName = activeBanks[1].AccountHolderName,
                    AccountNumber = activeBanks[1].AccountNumber,
                    IFSCCode = activeBanks[1].IFSCCode,
                    BranchName = activeBanks[1].BranchName,
                    IsActive = activeBanks[1].IsActive
                };
            }

            var paymentQrSetting = await _context.Settings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EntityType == "PaymentQR");

            var paymentUpiSetting = await _context.Settings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EntityType == "PaymentUPIId");

            order.ShowPaymentQR = paymentQrSetting != null && (paymentQrSetting.Value == "1" || paymentQrSetting.Value.ToLower() == "true");
            order.PaymentUPIId = paymentUpiSetting?.Value;

            return order;
        }

        public async Task<PagedResult<OrderResponseDto>> GetFilteredAsync(
    string? search,
    int pageNumber,
    int pageSize,
    string userId,
    string? role,
    int? statusFilter = null,
    DateTime? from = null,
    DateTime? to = null)
        {
            try
            {
                var userData = await _context.Users.FindAsync(userId);
                if (userData == null)
                    throw new Exception("User not found");
                var query = _context.Orders
                    .AsNoTracking()
                    .Where(o => !o.IsDeleted && o.TenantId == userData.TenantId);

                if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.CreatedBy == userId);
                }

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

                        InvoiceId = _context.Invoices
                            .Where(u => u.OrderID == o.OrderID.ToString())
                            .Select(u => u.InvoiceID)
                            .FirstOrDefault(),

                        ShippingAddress = o.ShippingAddress,
                        IsUseBillingAddress = o.IsUseBillingAddress,
                        StateID = o.StateID,
                        CityID = o.CityID,
                        QuotationID = o.QuotationID,
                        QuotationNo = _context.Quotations.Where(q=> q.QuotationID == o.QuotationID).Select(q => q.QuotationNo).FirstOrDefault(),
                        OrderDate = o.OrderDate,
                        IsDesignByUs = o.IsDesignByUs,
                        DesigningCharge = o.DesigningCharge,
                        ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                        Status = o.Status,
                        Taxes = o.TotalTaxes,
                        TotalAmount = o.SubTotal,
                        GrandTotal = o.GrandTotal,
                        isInvoiceCreated=o.isInvoiceCreated,
                        EnableTax = o.EnableTax,
                        CreatedDate = o.CreatedDate,

                        StatusName = _context.OrderStatusMasters
                            .Where(s => s.StatusID == o.Status)
                            .Select(s => s.StatusName)
                            .FirstOrDefault(),

                        DesignStatus = o.DesignStatusID,

                        DesignStatusName = _context.DesignStatusMasters
                            .Where(ds => ds.DesignStatusID == o.DesignStatusID)
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
                        OrderItems = _context.OrderItems
                            .Where(i => i.OrderID == o.OrderID)
                            .Join(_context.Products, oi => oi.ProductID, p => p.ProductID, (oi, p) => new { oi, p })
                            .GroupJoin(_context.TaxCategoryMasters, op => op.oi.TaxCategoryID, t => t.TaxCategoryID, (op, t) => new { op.oi, op.p, t })
                            .SelectMany(x => x.t.DefaultIfEmpty(), (x, t) => new OrderItemReponceDto
                            {
                                OrderItemID = x.oi.OrderItemID,
                                OrderID = x.oi.OrderID,
                                ProductID = x.oi.ProductID,
                                ProductName = x.p.ProductName,
                                Description = x.oi.Description,
                                Quantity = x.oi.Quantity,
                                UnitPrice = x.oi.UnitPrice,
                                TaxCategoryID = x.oi.TaxCategoryID ?? null,
                                TaxCategoryName = t != null ? t.TaxName : null,
                                LineTotal = x.oi.LineTotal,
                                Rate = t != null ? t.Rate : null,
                                HsnCode = x.p.HSNCode ?? null
                            })
                            .ToList(),
                    })
                    .ToListAsync();

                // 🔹 (Your existing WorkOrderItems loading logic stays SAME)
                // 🔹 (Your UTC → Local conversion stays SAME)

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
                var userData = await _context.Users.FindAsync(userId);

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


                Order? order;

                // ---------- CREATE ----------
                if (isNew)
                {
                    orderId = Guid.NewGuid();

                    order = new Order
                    {
                        OrderID = orderId,
                        OrderNo = await _numberGeneratorService.GenerateNumberAsync("OrderNo", userData?.TenantId?.ToString() ?? "00000000-0000-0000-0000-000000000000"),
                        ClientID = dto.ClientID ?? Guid.Empty,
                        QuotationID = dto.QuotationID,
                        OrderDate = dto.OrderDate ?? DateTime.Now,
                        IsDesignByUs = dto.IsDesignByUs ?? false,
                        DesigningCharge = dto.DesigningCharge ?? 0,
                        ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                        Status = dto.Status ?? 0,
                        FirmID = dto.FirmID,
                        DesignStatusID = dto.DesignStatus ?? 0,
                        CreatedBy = userId,
                        isInvoiceCreated = false,
                        EnableTax = dto.EnableTax.GetValueOrDefault(),

                        AssignedDesignTo = string.IsNullOrWhiteSpace(dto.AssignedDesignTo) ? null : dto.AssignedDesignTo,
                        IsUseBillingAddress = dto.IsUseBillingAddress.GetValueOrDefault(),
                        ShippingAddress = shippingAddress,
                        StateID = stateID,
                        CityID = cityID,
                        IsDeleted = false,
                        TenantId = userData?.TenantId
                    };

                    await _context.Orders.AddAsync(order);

                    // ===== MARK CLIENT AS CUSTOMER =====
                    var clientToUpdate = await _context.Clients
                        .FirstOrDefaultAsync(c => c.ClientID == order.ClientID);
                    if (clientToUpdate != null && !clientToUpdate.IsCustomer)
                    {
                        clientToUpdate.IsCustomer = true;
                        _context.Clients.Update(clientToUpdate);
                    }

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
                                lead.LeadStatusID = convertedStatusId;
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
                        order.DesignStatusID = dto.DesignStatus.Value;

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

                if (userData == null)
                {
                    throw new Exception("User not found or session expired.");
                }

                // ---------- RETURN SAVED RESPONSE ----------
                return await GetByIdAsync(orderId, userData.TenantId?.ToString() ?? "")
                    ?? throw new Exception("Saved but response fetch failed");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == id && !o.IsDeleted);
            if (order == null) return false;
            order.IsDeleted = true;
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
