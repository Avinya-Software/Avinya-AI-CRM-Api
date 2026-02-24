using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Shared.Model;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;

namespace AvinyaAICRM.Application.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly IHttpContextAccessor _http;

        public OrderService(IOrderRepository repo, IHttpContextAccessor http)
        {
            _repo = repo;
            _http = http;
        }

        #region COMMON USER VALIDATION

        private string GetUserId()
        {
            var userId = _http.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new Exception("Session expired. Please login again.");

            return userId;
        }

        #endregion

        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            try
            {
                var dto = await _repo.GetByIdAsync(id);

                if (dto == null)
                    return new ResponseModel(404, "Order not found");

                return CommonHelper.GetResponseMessage(dto);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> GetFilteredAsync(
            string? search,
            int page,
            int pageSize,
            int? statusFilter = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            try
            {
                var result = await _repo.GetFilteredAsync(
                    search, page, pageSize, statusFilter, from, to);

                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> AddOrUpdateAsync(OrderDto dto)
        {
            try
            {
                var userId = GetUserId();

                bool isNew = dto.OrderID == null || dto.OrderID == Guid.Empty;

                var saved = await _repo.AddOrUpdateOrderAsync(dto, userId);

                string message = isNew
                    ? "Order created successfully"
                    : "Order updated successfully";

                return CommonHelper.SuccessResponseMessage(message, saved);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> DeleteAsync(Guid id)
        {
            try
            {
                GetUserId();

                var ok = await _repo.SoftDeleteAsync(id);

                if (!ok)
                    return new ResponseModel(404, "Order not found");

                return CommonHelper.SuccessResponseMessage(
                    "Order deleted successfully",
                    null);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }
    }
}