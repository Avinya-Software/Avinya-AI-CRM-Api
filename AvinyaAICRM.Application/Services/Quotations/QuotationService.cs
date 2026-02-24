using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AvinyaAICRM.Application.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly IQuotationRepository _quotationRepository;
        private readonly IHttpContextAccessor _http;

        public QuotationService(
            IQuotationRepository repo,
            IHttpContextAccessor http)
        {
            _quotationRepository = repo;
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

        // ✅ Get All
        public async Task<ResponseModel> GetAllAsync()
        {
            try
            {
                var data = await _quotationRepository.GetAllAsync();
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
                var data = await _quotationRepository.GetByIdAsync(id);

                if (data == null)
                    return new ResponseModel(404, "Quotation not found");

                return CommonHelper.GetResponseMessage(data);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Add / Update
        public async Task<ResponseModel> AddOrUpdateAsync(QuotationRequestDto dto)
        {
            try
            {
                GetUserId();

                var (result, isNew) =
                    await _quotationRepository.PostOrPutAsync(dto);

                string message = isNew
                    ? "Quotation created successfully."
                    : "Quotation updated successfully.";

                return CommonHelper.SuccessResponseMessage(message, result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Soft Delete
        public async Task<ResponseModel> SoftDeleteAsync(Guid id)
        {
            try
            {
                GetUserId();

                var deleted = await _quotationRepository.SoftDeleteAsync(id);

                if (!deleted)
                    return new ResponseModel(404, "Quotation not found");

                return CommonHelper.SuccessResponseMessage(
                    "Quotation deleted successfully",
                    null);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Filter + Pagination
        public async Task<ResponseModel> FilterAsync(
            string? search,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize)
        {
            try
            {
                var result = await _quotationRepository
                    .FilterAsync(search, statusFilter, startDate, endDate, page, pageSize);

                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }
    }
}