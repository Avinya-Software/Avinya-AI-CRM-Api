using AvinyaAICRM.Application.DTOs.Product;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Products;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Products;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AvinyaAICRM.Application.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(
            IProductRepository repository,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        #region COMMON USER VALIDATION

        private string GetUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new Exception("Session expired. Please login again.");

            return userId;
        }

        #endregion

        // ✅ Get All
        public async Task<ResponseModel> GetAllAsync()
        {
            try
            {
                var products = await _repository.GetAllAsync();
                return CommonHelper.GetResponseMessage(products);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Get By Id
        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            try
            {
                var product = await _repository.GetByIdAsync(id);

                if (product == null)
                    return new ResponseModel(404, "Product not found");

                return CommonHelper.GetResponseMessage(product);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Create
        public async Task<ResponseModel> CreateAsync(ProductRequest product)
        {
            try
            {
                GetUserId();

                if (product.ProductID == Guid.Empty)
                    product.ProductID = Guid.NewGuid();

                var created = await _repository.AddAsync(product);

                return CommonHelper.SuccessResponseMessage(
                    "Product created successfully",
                    created);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Update
        public async Task<ResponseModel> UpdateAsync(ProductDto productDto)
        {
            try
            {
                GetUserId();

                var updated = await _repository.UpdateAsync(productDto);

                if (updated == null)
                    return new ResponseModel(404, "Product not found");

                return CommonHelper.SuccessResponseMessage(
                    "Product updated successfully",
                    updated);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Delete
        public async Task<ResponseModel> DeleteAsync(Guid id)
        {
            try
            {
                GetUserId();

                var deleted = await _repository.DeleteAsync(id);

                if (!deleted)
                    return new ResponseModel(404, "Product not found");

                return CommonHelper.SuccessResponseMessage(
                    "Product deleted successfully",
                    null);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Filter + Pagination
        public async Task<ResponseModel> GetFilteredAsync(
            string? search,
            bool? status,
            int page,
            int pageSize)
        {
            try
            {
                var result =
                    await _repository.GetFilteredAsync(search, status, page, pageSize);

                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Unit Type Dropdown
        public async Task<ResponseModel> GetUnitTypeAsync()
        {
            try
            {
                var result = await _repository.GetUnitTypeAsync();
                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }
    }
}