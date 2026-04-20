using AvinyaAICRM.Application.DTOs.Product;
using AvinyaAICRM.Domain.Entities.Master;
using AvinyaAICRM.Domain.Entities.Product;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Products
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductDropDown>> GetAllAsync(string userid);

        Task<ProductDto?> GetByIdAsync(Guid id);
        Task<ProductRequest> AddAsync(ProductRequest product);
        Task<Product?> UpdateAsync(ProductDto productDto);
        Task<bool> DeleteAsync(Guid id, string userid);
        Task<PagedResult<ProductDto>> GetFilteredAsync(
             string? search,
             bool? status,
             int pageNumber,
             int pageSize,
             string userId);
        Task<IEnumerable<UnitType>> GetUnitTypeAsync();
    }
}
