using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AvinyaAICRM.Application.Services.Leads
{
    public class LeadFollowupService : ILeadFollowupService
    {
        private readonly ILeadFollowupRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeadFollowupService(
            ILeadFollowupRepository repository,
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

        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            try
            {
                var data = await _repository.GetByIdAsync(id);

                if (data == null)
                    return new ResponseModel(404, "Follow-up not found");

                return CommonHelper.GetResponseMessage(data);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> CreateAsync(LeadFollowups dto)
        {
            try
            {

                var (data, error) = await _repository.AddAsync(dto);

                if (error != null)
                    return new ResponseModel(400, error);

                return CommonHelper.SuccessResponseMessage(
                    "Follow-up created successfully",
                    data);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> UpdateAsync(LeadFollowups dto)
        {
            try
            {
                GetUserId();

                var updatedEntity = await _repository.UpdateAsync(dto);

                if (updatedEntity == null)
                    return new ResponseModel(
                        400,
                        "Update not allowed. This follow-up is already completed.");

                return CommonHelper.SuccessResponseMessage(
                    "Follow-up updated successfully",
                    updatedEntity);
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

                var deleted = await _repository.DeleteAsync(id);

                if (!deleted)
                    return new ResponseModel(404, "Follow-up not found");

                return CommonHelper.SuccessResponseMessage(
                    "Follow-up deleted successfully",
                    null);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> GetFilteredAsync(
            string? search,
            string? status,
            Guid? leadId,
            int page,
            int pageSize)
        {
            try
            {
                var result = await _repository
                    .GetFilteredAsync(search, status, leadId, page, pageSize);

                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> GetFollowupHistoryAsync(Guid leadId)
        {
            try
            {
                var (leadExists, followups) =
                    await _repository.GetFollowupHistoryAsync(leadId);

                if (!leadExists)
                    return new ResponseModel(404, "Lead not found");

                return CommonHelper.GetResponseMessage(followups);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> GetAllLeadFollowupStatusesAsync()
        {
            try
            {
                var data = await _repository.GetLeadFollowupStatusAsync();
                return CommonHelper.GetResponseMessage(data);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }
    }
}