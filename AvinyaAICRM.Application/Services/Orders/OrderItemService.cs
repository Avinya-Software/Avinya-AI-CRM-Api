using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AvinyaAICRM.Application.Services.Orders
{
    public class OrderItemService : IOrderItemService
    {
        private readonly IOrderItemRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderItemService(
            IOrderItemRepository repository,
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
                var data = await _repository.GetAllAsync();
                return CommonHelper.GetResponseMessage(data);
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
                var item = await _repository.GetByIdAsync(id);

                if (item == null)
                    return new ResponseModel(404, "Order item not found");

                return CommonHelper.GetResponseMessage(item);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Create
        public async Task<ResponseModel> CreateAsync(OrderItemDto dto)
        {
            try
            {
                GetUserId();

                var entity = new OrderItem
                {
                    OrderID = dto.OrderID,
                    ProductID = dto.ProductID,
                    Description = dto.Description,
                    Quantity = dto.Quantity,
                    UnitPrice = dto.UnitPrice,
                    TaxCategoryID = dto.TaxCategoryID,
                    LineTotal = dto.UnitPrice * dto.Quantity
                };

                var created = await _repository.CreateAsync(entity);
                var detailed = await _repository.GetByIdAsync(created.OrderItemID);

                return CommonHelper.SuccessResponseMessage(
                    "Order item created successfully",
                    detailed);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Update
        public async Task<ResponseModel> UpdateAsync(Guid id, OrderItemDto dto)
        {
            try
            {
                GetUserId();

                var entity = new OrderItem
                {
                    OrderItemID = id,
                    Description = dto.Description,
                    Quantity = dto.Quantity,
                    UnitPrice = dto.UnitPrice,
                    TaxCategoryID = dto.TaxCategoryID
                };

                var updated = await _repository.UpdateAsync(entity);

                if (updated == null)
                    return new ResponseModel(404, "Order item not found");

                var detailed = await _repository.GetByIdAsync(id);

                return CommonHelper.SuccessResponseMessage(
                    "Order item updated successfully",
                    detailed);
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
                    return new ResponseModel(404, "Order item not found");

                return CommonHelper.SuccessResponseMessage(
                    "Order item deleted successfully",
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
            int page,
            int pageSize)
        {
            try
            {
                var result = await _repository.GetFilteredAsync(search, page, pageSize);

                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }
    }
}