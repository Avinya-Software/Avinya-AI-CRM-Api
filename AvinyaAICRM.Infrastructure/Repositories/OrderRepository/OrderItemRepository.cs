using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;
using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.OrderRepository
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly AppDbContext _context;

        public OrderItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrderItemReponceDto>> GetAllAsync()
        {
            return await _context.OrderItems
                .Include(o => o.Product)
                .Include(o => o.TaxCategory)
                .Select(o => new OrderItemReponceDto
                {
                    OrderItemID = o.OrderItemID,
                    OrderID = o.OrderID,
                    ProductID = o.ProductID,
                    ProductName = o.Product.ProductName,
                    Description = o.Description,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    TaxCategoryID = o.TaxCategoryID,
                    TaxCategoryName = o.TaxCategory != null ? o.TaxCategory.TaxName : null,
                    LineTotal = o.LineTotal
                })
                .OrderByDescending(o => o.OrderItemID)
                .ToListAsync();
        }

        public async Task<OrderItemReponceDto?> GetByIdAsync(Guid id)
        {
            return await _context.OrderItems
                .Include(o => o.Product)
                .Include(o => o.TaxCategory)
                .Where(o => o.OrderItemID == id)
                .Select(o => new OrderItemReponceDto
                {
                    OrderItemID = o.OrderItemID,
                    OrderID = o.OrderID,
                    ProductID = o.ProductID,
                    ProductName = o.Product.ProductName,
                    Description = o.Description,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    TaxCategoryID = o.TaxCategoryID,
                    TaxCategoryName = o.TaxCategory != null ? o.TaxCategory.TaxName : null,
                    LineTotal = o.LineTotal
                })
                .FirstOrDefaultAsync();
        }

        public async Task<OrderItem> CreateAsync(OrderItem item)
        {
            item.LineTotal = item.Quantity * item.UnitPrice;
            _context.OrderItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<OrderItem?> UpdateAsync(OrderItem dto)
        {
            try
            {
                var existing = await _context.OrderItems
                    .FirstOrDefaultAsync(o => o.OrderItemID == dto.OrderItemID);

                if (existing == null)
                    return null;

                if (dto.ProductID != Guid.Empty && dto.TaxCategoryID != null)
                {
                    bool exists = await _context.Products.AnyAsync(p => p.ProductID == dto.ProductID);
                    if (exists)
                        existing.ProductID = dto.ProductID;
                }

                if (dto.TaxCategoryID != null && dto.TaxCategoryID != Guid.Empty)
                {
                    bool exists = await _context.TaxCategoryMasters.AnyAsync(t => t.TaxCategoryID == dto.TaxCategoryID);
                    if (exists)
                        existing.TaxCategoryID = dto.TaxCategoryID;
                }

                if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description != "string")
                    existing.Description = dto.Description.Trim();

                if (dto.UnitPrice > 0)
                    existing.UnitPrice = dto.UnitPrice;

                if (dto.Quantity > 0)
                    existing.Quantity = dto.Quantity;
                existing.LineTotal = existing.UnitPrice * existing.Quantity;

                await _context.SaveChangesAsync();

                var updated = await _context.OrderItems
                    .Include(o => o.Product)
                    .Include(o => o.TaxCategory)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderItemID == existing.OrderItemID);

                return updated;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating OrderItem: {ex.Message}");
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.OrderItems.FindAsync(id);
            if (existing == null) return false;

            _context.OrderItems.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<PagedResult<OrderItemReponceDto>> GetFilteredAsync(
      string? search,
      int pageNumber,
      int pageSize)
        {
            var query =
                from oItem in _context.OrderItems.AsNoTracking()
                join o in _context.Orders on oItem.OrderID equals o.OrderID
                join p in _context.Products on oItem.ProductID equals p.ProductID
                join t in _context.TaxCategoryMasters
                    on oItem.TaxCategoryID equals t.TaxCategoryID into taxJoin
                from tax in taxJoin.DefaultIfEmpty()

                select new OrderItemReponceDto
                {
                    OrderItemID = oItem.OrderItemID,
                    OrderID = oItem.OrderID,
                    ProductID = oItem.ProductID,
                    ProductName = p.ProductName,
                    Description = oItem.Description,
                    Quantity = oItem.Quantity,
                    UnitPrice = oItem.UnitPrice,
                    TaxCategoryID = oItem.TaxCategoryID,
                    TaxCategoryName = tax != null ? tax.TaxName : null,
                    LineTotal = oItem.LineTotal
                };

            #region Search Filter

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
                .OrderByDescending(x => x.OrderItemID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ✅ Return PagedResult
            return new PagedResult<OrderItemReponceDto>
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