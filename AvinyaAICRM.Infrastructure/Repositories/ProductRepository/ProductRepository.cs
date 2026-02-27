using AvinyaAICRM.Application.DTOs.Product;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Products;
using AvinyaAICRM.Domain.Entities.Master;
using AvinyaAICRM.Domain.Entities.Product;
using AvinyaAICRM.Domain.Entities.TaxCategory;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;



namespace AvinyaAICRM.Infrastructure.Repositories.ProductRepository
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductRepository(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        //  Get all clients
        public async Task<IEnumerable<ProductDropDown>> GetAllAsync(string userid)
        {
            var userData = await _context.Users.FindAsync(userid);

            var unitTypes = await _context.UnitTypeMasters
                .Select(u => new { u.UnitTypeID, u.UnitName })
                .ToListAsync();

            var taxCategories = await _context.TaxCategoryMasters
                .Select(t => new { t.TaxCategoryID, t.TaxName })
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.Status == 1 && !p.IsDeleted && p.TenantId == userData.TenantId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var result = (from p in products
                          let parsedUnitTypeId = Guid.TryParse(p.UnitType, out var uid) ? uid : Guid.Empty
                          join u in unitTypes
                              on parsedUnitTypeId equals u.UnitTypeID into ug
                          from u in ug.DefaultIfEmpty()

                          join t in taxCategories
                              on p.TaxCategoryID equals t.TaxCategoryID into tg
                          from t in tg.DefaultIfEmpty()

                          select new ProductDropDown
                          {
                              ProductID = p.ProductID,
                              ProductName = p.ProductName,
                              Description = p.Description,
                              //Category = p.Category,
                              //UnitTypeId = p.UnitType,          
                              UnitName = u?.UnitName
                              //DefaultRate = p.DefaultRate,
                              //PurchasePrice = p.PurchasePrice,
                              //HSNCode = p.HSNCode,
                              //IsDesignByUs = p.IsDesignByUs,
                              //Description = p.Description,
                              //Status = p.Status,
                              //CreatedByID = p.CreatedBy,
                              //CreatedDate = p.CreatedDate,
                              //UpdatedAt = p.UpdatedAt,
                              //TaxCategoryID = p.TaxCategoryID,
                              //TaxCategoryName = t?.TaxName
                          }).ToList();

            return result;
        }


        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var unitTypes = await _context.UnitTypeMasters
                .Select(u => new { u.UnitTypeID, u.UnitName })
                .ToListAsync();

            var taxCategories = await _context.TaxCategoryMasters
                .Select(t => new { t.TaxCategoryID, t.TaxName })
                .ToListAsync();

            var product = await _context.Products.Where(x => !x.IsDeleted)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
                return null;

            var unitType = unitTypes.FirstOrDefault(u =>
                Guid.TryParse(product.UnitType, out var uid) && u.UnitTypeID == uid);

            var taxCategory = taxCategories.FirstOrDefault(t =>
                t.TaxCategoryID == product.TaxCategoryID);
            var createdByUser = await _context.Users
                .Where(u => u.Id == product.CreatedBy)
                .Select(u => new { u.UserName })   
                .FirstOrDefaultAsync();
            return new ProductDto
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Category = product.Category,
                UnitTypeId = product.UnitType,
                UnitTypeName = unitType?.UnitName,
                DefaultRate = product.DefaultRate,
                PurchasePrice = product.PurchasePrice,
                HSNCode = product.HSNCode,
                IsDesignByUs = product.IsDesignByUs,
                Description = product.Description,
                Status = product.Status,
                CreatedByID = product.CreatedBy,
                CreatedByName = createdByUser?.UserName ?? "",
                CreatedDate = product.CreatedDate,
                UpdatedAt = product.UpdatedAt,
                TaxCategoryID = product.TaxCategoryID,
                TaxCategoryName = taxCategory?.TaxName
            };
        }

        public async Task<ProductRequest> AddAsync(ProductRequest dto)
        {
            if (dto.ProductID == Guid.Empty)
                dto.ProductID = Guid.NewGuid();

            var userData = await _context.Users.FindAsync(dto.CreatedBy);

            // Map DTO → Entity
            var entity = new Product
            {
                ProductName = dto.ProductName,
                Category = dto.Category,
                UnitType = dto.UnitType,
                DefaultRate = dto.DefaultRate,
                PurchasePrice = dto.PurchasePrice,
                HSNCode = dto.HSNCode,
                TaxCategoryID = dto.TaxCategoryID,
                IsDesignByUs = dto.IsDesignByUs,
                Description = dto.Description,
                Status = dto.Status,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                TenantId = userData.TenantId
            };


            if (dto.TaxCategoryID != null)
            {
                _context.Attach(new TaxCategoryMaster
                {
                    TaxCategoryID = dto.TaxCategoryID.Value
                }).State = EntityState.Unchanged;
            }

            _context.Products.Add(entity);
            await _context.SaveChangesAsync();

            return dto;
        }


        //  Update client
        public async Task<Product?> UpdateAsync(ProductDto dto)
        {
            try
            {
                var existing = await _context.Products
     .FirstOrDefaultAsync(x => x.ProductID == dto.ProductID && !x.IsDeleted);

                if (existing == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(dto.ProductName))
                    existing.ProductName = dto.ProductName;

                if (dto.Category != null)
                    existing.Category = dto.Category.Trim();
                else
                    existing.Category = string.Empty;

                if (dto.HSNCode != null)
                    existing.HSNCode = dto.HSNCode.Trim();
                else
                    existing.HSNCode = string.Empty;

                if (dto.Description != null)
                    existing.Description = dto.Description.Trim();
                else
                    existing.Description = string.Empty;

                existing.IsDesignByUs = dto.IsDesignByUs;


                if (dto.UnitTypeId != null)
                    existing.UnitType = dto.UnitTypeId.Trim();
                else
                    existing.UnitType = string.Empty;

                if (dto.TaxCategoryID.HasValue)
                    existing.TaxCategoryID = dto.TaxCategoryID.Value;
                else
                    existing.TaxCategoryID = null;

                if (dto.DefaultRate.HasValue)
                    existing.DefaultRate = dto.DefaultRate.Value;
                else
                    existing.DefaultRate = null;

                if (dto.PurchasePrice.HasValue)
                    existing.PurchasePrice = dto.PurchasePrice.Value;
                else
                    existing.PurchasePrice = null;

                existing.Status = dto.Status;

                existing.UpdatedAt = DateTime.Now;
                _context.Products.Update(existing);
                await _context.SaveChangesAsync();

                return existing;
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        //  Delete client
        public async Task<bool> DeleteAsync(Guid id, string userId)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null)
                return false;

            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("Session expired. Please login again.");
            }

            existing.IsDeleted = true;
            existing.DeletedDate = DateTime.Now;
            existing.DeletedBy = userId;

            _context.Products.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        //  Filter + Pagination
        public async Task<PagedResult<ProductDto>> GetFilteredAsync(
     string? search,
     bool? status,
     int pageNumber,
     int pageSize,
     string userId)
        {
            var userData = await _context.Users.FindAsync(userId);

            var query =
                from p in _context.Products.AsNoTracking()
                join ut in _context.UnitTypeMasters
                    on p.UnitType equals ut.UnitTypeID.ToString() into utJoin
                from ut in utJoin.DefaultIfEmpty()
                where !p.IsDeleted && p.TenantId == userData.TenantId
                select new { p, ut };

            #region Search Filter

            if (!string.IsNullOrWhiteSpace(search))
            {
                var likeSearch = $"%{search}%";

                query = query.Where(x =>
                    EF.Functions.Like(x.p.ProductName, likeSearch) ||
                    EF.Functions.Like(x.p.Category, likeSearch) ||
                    EF.Functions.Like(x.p.HSNCode, likeSearch));
            }

            #endregion

            #region Status Filter

            if (status.HasValue)
                query = query.Where(x => x.p.Status == (status.Value ? 1 : 0));

            #endregion

            // ✅ Total Records
            var totalRecords = await query.CountAsync();

            // ✅ Paged Data
            var products = await query
                .OrderByDescending(x => x.p.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductDto
                {
                    ProductID = x.p.ProductID,
                    ProductName = x.p.ProductName,
                    Category = x.p.Category,

                    UnitTypeId = x.p.UnitType,
                    UnitTypeName = x.ut != null ? x.ut.UnitName : null,

                    DefaultRate = x.p.DefaultRate,
                    PurchasePrice = x.p.PurchasePrice,
                    HSNCode = x.p.HSNCode,
                    TaxCategoryID = x.p.TaxCategoryID,
                    IsDesignByUs = x.p.IsDesignByUs,
                    Description = x.p.Description,
                    Status = x.p.Status,
                    CreatedByID = x.p.CreatedBy,
                    CreatedDate = x.p.CreatedDate,
                    UpdatedAt = x.p.UpdatedAt
                })
                .ToListAsync();

            // ✅ Return PagedResult
            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = products
            };
        }

        public async Task<IEnumerable<UnitType>> GetUnitTypeAsync()
        {
            return await _context.UnitTypeMasters.ToListAsync();
        }
    }

}