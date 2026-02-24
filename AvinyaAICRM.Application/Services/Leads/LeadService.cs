using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace AvinyaAICRM.Application.Services.Leads
{
    public class LeadService : ILeadService
    {
        private readonly ILeadRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeadService(ILeadRepository repository, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseModel> GetAllAsync()
        {
            try
            {
                var leads = await _repository.GetAllAsync();
                return CommonHelper.GetResponseMessage(leads);
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
                var lead = await _repository.GetByIdAsync(id);
                if (lead == null)
                    return new ResponseModel(404, "Lead not found");

                return CommonHelper.GetResponseMessage(lead);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> CreateAsync(LeadRequestDto dto, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("Session expired. Please login again.");
                }

                var validation = await _repository.ValidateClientAsync(dto);


                if (!validation.IsValid)
                    return new ResponseModel(400, validation.Message);

                var created = await _repository.AddAsync(dto, userId);

                return CommonHelper.SuccessResponseMessage("lead Created Succesfully ",created);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> UpdateAsync(LeadRequestDto dto)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("Session expired. Please login again.");
                }

                var validation = await _repository.ValidateClientAsync(dto);


                if (!validation.IsValid)
                    return new ResponseModel(400, validation.Message);

                var id = dto.LeadID;

                if (id == null)
                    return new ResponseModel(400, "LeadID is required");

                var oldData = await _repository.GetByIdAsync(dto.LeadID.Value);

                var updated = await _repository.UpdateAsync(dto);
                if (updated == null)
                    return new ResponseModel(404, "Lead not found");

                return CommonHelper.SuccessResponseMessage("Lead updated successfully ", updated);                
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> UpdateLeadStatus(Guid id, Guid statusId)
        {
            if (id == Guid.Empty)
            {
                return new ResponseModel
                {
                    StatusCode = 400,
                    StatusMessage = "LeadId is missing.",
                    Data = null
                };

            }

            var existingLead = await _repository.GetLeadByIdAsync(id);
            if (existingLead != null)
            {
                existingLead.Status = statusId.ToString();
            }

            var result = await _repository.UpdateLeadStatusAsync(existingLead);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> DeleteAsync(Guid id, string deletedBy)
        {
            try
            {
                var oldClient = await _repository.GetByIdAsync(id);

                var deleted = await _repository.DeleteAsync(id,deletedBy);
                if (!deleted)
                    return new ResponseModel(404, "Lead not found");
                return CommonHelper.SuccessResponseMessage("Lead deleted successfully", null);

            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }


        public async Task<ResponseModel> GetFilteredAsync(
            string? search,
            string? statusId, DateTime? startDate, DateTime? endDate,
            int page,
            int pageSize,
            ClaimsPrincipal user)
        {
            var result = await _repository.GetFilteredAsync(search, statusId, startDate,endDate, page, pageSize, user);

            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetAllSourceAsync()
        {
            var data = await _repository.GetAllLeadSourceAsync();
            return CommonHelper.GetResponseMessage(data);
        }
        public async Task<ResponseModel> GetAllStatusAsync()
        {
            var data = await _repository.GetAllLeadStatusAsync();
            return CommonHelper.GetResponseMessage(data);
        }

        public async Task<ResponseModel> GetLeadHistory(Guid leadId)
        {
            var data = await _repository.GetLeadHistoryAsync(leadId);

            return CommonHelper.GetResponseMessage(data);
        }

        public async Task<ResponseModel> GetAllLeadGrpByStatus()
        {
            var data = await _repository.GetAllLeadGrpByStatus();

            return CommonHelper.GetResponseMessage(data);
        }

    }
}