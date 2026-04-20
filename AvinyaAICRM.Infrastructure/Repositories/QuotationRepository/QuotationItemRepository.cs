using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations;
using AvinyaAICRM.Domain.Entities.Quotations;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;


namespace AvinyaAICRM.Infrastructure.Repositories.QuotationRepository
{
    public class QuotationItemRepository : IQuotationItemRepository
    {
        private readonly AppDbContext _context;

        public QuotationItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QuotationItemResponseDto>> GetAllAsync(Guid? quotationId = null)
        {
            bool enableTax = true;

            if (quotationId.HasValue && quotationId.Value != Guid.Empty)
            {
                enableTax = await _context.Quotations
                    .Where(q => q.QuotationID == quotationId.Value)
                    .Select(q => q.EnableTax)
                    .FirstOrDefaultAsync();
            }

            var products = await _context.Products
                .Select(p => new { p.ProductID, p.ProductName })
                .ToListAsync();

            var taxCategories = _context.TaxCategoryMasters
                .Select(t => new
                {
                    t.TaxCategoryID,
                    t.TaxName,
                    t.Rate   // ✅ ADD THIS
                })
            .ToList();


            // ✅ Build query
            var query = _context.QuotationItems.AsQueryable();

            // ✅ Apply filter if QuotationID provided
            if (quotationId.HasValue && quotationId.Value != Guid.Empty)
            {
                query = query.Where(i => i.QuotationID == quotationId.Value);
            }

            var items = await query.ToListAsync();

            return items.Select(i =>
            {
                var tax = taxCategories.FirstOrDefault(t => t.TaxCategoryID == i.TaxCategoryID);

                return new QuotationItemResponseDto
                {
                    QuotationItemID = i.QuotationItemID,
                    QuotationID = i.QuotationID,
                    ProductID = i.ProductID,
                    ProductName = products.FirstOrDefault(p => p.ProductID == i.ProductID)?.ProductName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,

                    // ✅ EnableTax logic applied
                    TaxCategoryID = enableTax ? i.TaxCategoryID : null,
                    TaxCategoryName = enableTax ? tax?.TaxName : "N/A",
                    Rate = enableTax ? (tax?.Rate ?? 0) : 0,
                    LineTotal = i.LineTotal
                };
            }).ToList();
        }


        public async Task<QuotationItemResponseDto?> GetByIdAsync(Guid id)
        {
            var item = await _context.QuotationItems
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuotationItemID == id);

            if (item == null)
                return null;

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

            var taxCategory = item.TaxCategoryID != Guid.Empty
                ? await _context.TaxCategoryMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TaxCategoryID == item.TaxCategoryID)
                : null;

            return new QuotationItemResponseDto
            {
                QuotationItemID = item.QuotationItemID,
                QuotationID = item.QuotationID,
                ProductID = item.ProductID,
                ProductName = product?.ProductName ?? "N/A",
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxCategoryID = item.TaxCategoryID,
                TaxCategoryName = taxCategory?.TaxName ?? "N/A",
                LineTotal = item.LineTotal
            };
        }


        public async Task AddAsync(QuotationItem item)
        {
            item.QuotationItemID = Guid.NewGuid();
            item.LineTotal = item.Quantity * item.UnitPrice;

            var quotation = await _context.Quotations
                .OrderByDescending(q => q.CreatedDate)
                .FirstOrDefaultAsync();

            if (quotation == null)
                throw new Exception("No quotation found to attach this item to.");

            if (item.QuotationID != Guid.Empty && item.QuotationID != null)
            {
                var specificQuotation = await _context.Quotations.FindAsync(item.QuotationID);

                if (specificQuotation != null)
                    quotation = specificQuotation;
            }

            item.QuotationID = quotation.QuotationID;
            await _context.QuotationItems.AddAsync(item);
            await _context.SaveChangesAsync();

            quotation.TotalAmount = await _context.QuotationItems
                .Where(x => x.QuotationID == quotation.QuotationID)
                .SumAsync(x => x.LineTotal);

            quotation.GrandTotal = quotation.TotalAmount + quotation.Taxes;

            _context.Quotations.Update(quotation);
            await _context.SaveChangesAsync();
        }
        public async Task<QuotationItem> UpdateAsync(QuotationItem item)
        {
            var existing = await _context.QuotationItems
                .FirstOrDefaultAsync(q => q.QuotationItemID == item.QuotationItemID);

            if (existing == null)
                throw new Exception($"No QuotationItem found for ID: {item.QuotationItemID}");

            if (!string.IsNullOrWhiteSpace(item.Description) && item.Description != "string")
                existing.Description = item.Description.Trim();

            if (item.UnitPrice > 0)
                existing.UnitPrice = item.UnitPrice;

            if (item.Quantity > 0)
                existing.Quantity = item.Quantity;


            if (item.TaxCategoryID != Guid.Empty &&
               item.TaxCategoryID != existing.TaxCategoryID
               )
            {
                var taxExists = await _context.TaxCategoryMasters
                    .AnyAsync(t => t.TaxCategoryID == item.TaxCategoryID);

                if (taxExists)
                    existing.TaxCategoryID = item.TaxCategoryID;
            }


            existing.LineTotal = (existing.UnitPrice * existing.Quantity);

            _context.QuotationItems.Update(existing);
            await _context.SaveChangesAsync();

            return await _context.QuotationItems
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuotationItemID == existing.QuotationItemID)
                ?? throw new Exception("Failed to fetch updated quotation item.");
        }


        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.QuotationItems.FindAsync(id);
            if (existing != null)
            {
                _context.QuotationItems.Remove(existing);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;

        }
        public async Task<PagedResult<QuotationItemResponseDto>> GetFilteredAsync(
     string? search,
     Guid? statusId,
     int pageNumber,
     int pageSize)
        {
            var query =
                from qItem in _context.QuotationItems.AsNoTracking()
                join q in _context.Quotations on qItem.QuotationID equals q.QuotationID
                join p in _context.Products on qItem.ProductID equals p.ProductID
                join t in _context.TaxCategoryMasters
                    on qItem.TaxCategoryID equals t.TaxCategoryID into taxJoin
                from tax in taxJoin.DefaultIfEmpty()

                select new QuotationItemResponseDto
                {
                    QuotationItemID = qItem.QuotationItemID,
                    QuotationID = qItem.QuotationID,
                    ProductID = qItem.ProductID,
                    ProductName = p.ProductName,
                    TaxCategoryID = qItem.TaxCategoryID,
                    TaxCategoryName = tax != null ? tax.TaxName : null,
                    Description = qItem.Description,
                    Quantity = qItem.Quantity,
                    UnitPrice = qItem.UnitPrice,
                    LineTotal = qItem.LineTotal
                };

            #region Search Filter (DB LEVEL — Faster)

            if (!string.IsNullOrWhiteSpace(search))
            {
                var likeSearch = $"%{search}%";

                query = query.Where(x =>
                    EF.Functions.Like(x.ProductName, likeSearch) ||
                    EF.Functions.Like(x.TaxCategoryName, likeSearch) ||
                    EF.Functions.Like(x.Description, likeSearch));
            }

            #endregion

            // ✅ Total Records
            var totalRecords = await query.CountAsync();

            // ✅ Paging
            var items = await query
                .OrderByDescending(x => x.QuotationItemID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ✅ Return PagedResult
            return new PagedResult<QuotationItemResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = items
            };
        }

    }
}

