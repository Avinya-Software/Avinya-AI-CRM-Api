using AvinyaAICRM.Domain.Entities.TaxCategory;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.TaxCategories
{
    public interface ITaxCategoryRepository
    {
        Task<IEnumerable<TaxCategoryMaster>> GetAllAsync();
        Task<TaxCategoryMaster?> GetByIdAsync(Guid id);
    }
}
