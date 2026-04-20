using AvinyaAICRM.Application.Interfaces.RepositoryInterface.TaxCategories;
using AvinyaAICRM.Domain.Entities.TaxCategory;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.TaxCategoryRepository
{
    public class TaxCategoryRepository : ITaxCategoryRepository
    {
        private readonly AppDbContext _context;

        public TaxCategoryRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<TaxCategoryMaster>> GetAllAsync()
        {
            var taxCategories = await _context.TaxCategoryMasters
                .OrderByDescending(t => t.TaxName)
                .Select(t => new TaxCategoryMaster
                {
                    TaxCategoryID = t.TaxCategoryID,
                    TaxName = t.TaxName,
                    Rate = t.Rate,
                    IsCompound = t.IsCompound
                })
                .ToListAsync();

            return taxCategories;
        }

        public async Task<TaxCategoryMaster?> GetByIdAsync(Guid id)
        {
            var t = await _context.TaxCategoryMasters
                .FirstOrDefaultAsync(tc => tc.TaxCategoryID == id);

            if (t == null)
                return null;

            return new TaxCategoryMaster
            {
                TaxCategoryID = t.TaxCategoryID,
                TaxName = t.TaxName,
                Rate = t.Rate,
                IsCompound = t.IsCompound
            };
        }
    }
}
