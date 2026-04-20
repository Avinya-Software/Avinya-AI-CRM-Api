using AvinyaAICRM.Application.DTOs.Product;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Products
{
    public interface IProductService
    {
        Task<ResponseModel> GetAllAsync(string userid);
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(ProductRequest product);
        Task<ResponseModel> UpdateAsync(ProductDto productDto);
        Task<ResponseModel> DeleteAsync(Guid id, string userid);
        Task<ResponseModel> GetFilteredAsync(string? search, bool? status, int page, int pageSize, string userId);

        Task<ResponseModel> GetUnitTypeAsync(); 

    }

}
