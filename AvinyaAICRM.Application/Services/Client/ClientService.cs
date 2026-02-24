using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Client;
using AvinyaAICRM.Application.Interfaces.Clients;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AvinyaAICRM.Application.Services.Client
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientService(
            IClientRepository repository,
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

        public async Task<ResponseModel> GetAllAsync(bool getAll = false)
        {
            try
            {
                var clients = await _repository.GetAllAsync(getAll);
                return CommonHelper.GetResponseMessage(clients);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> GetByIdAsync(Guid clientId)
        {
            try
            {
                var client = await _repository.GetByIdAsync(clientId);

                if (client == null)
                    return new ResponseModel(404, "Client not found");

                return CommonHelper.GetResponseMessage(client);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> CreateAsync(ClientRequestDto dto)
        {
            try
            {
                var userId = GetUserId();

                var gst = string.IsNullOrWhiteSpace(dto.GSTNo) ? null : dto.GSTNo.Trim();
                var mobile = string.IsNullOrWhiteSpace(dto.Mobile) ? null : dto.Mobile.Trim();
                var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

                var duplicates =
                    await _repository.CheckClientDuplicatesAsync(gst, mobile, email);

                if (duplicates.mobileExists)
                    return new ResponseModel(400, "Mobile number already exists");

                if (duplicates.emailExists)
                    return new ResponseModel(400, "Email already exists");

                if (duplicates.gstExists)
                    return new ResponseModel(400, "GST No already exists");

                var client = new Domain.Entities.Client.Client
                {
                    ClientID = Guid.NewGuid(),
                    CompanyName = dto.CompanyName,
                    ContactPerson = dto.ContactPerson,
                    Mobile = mobile,
                    Email = email,
                    GSTNo = gst,
                    BillingAddress = dto.BillingAddress,
                    ClientType = dto.ClientType,
                    Status = dto.Status,
                    StateID = dto.StateID,
                    CityID = dto.CityID,
                    Notes = dto.Notes,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var created = await _repository.AddAsync(client);

                return CommonHelper.SuccessResponseMessage(
                    "Client created successfully",
                    created);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> UpdateAsync(ClientRequestDto dto)
        {
            try
            {
                GetUserId();

                var oldClient = await _repository.GetByIdAsync(dto.ClientID);
                if (oldClient == null)
                    return new ResponseModel(404, "Client not found");

                var gst = string.IsNullOrWhiteSpace(dto.GSTNo) ? null : dto.GSTNo.Trim();
                var mobile = string.IsNullOrWhiteSpace(dto.Mobile) ? null : dto.Mobile.Trim();
                var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

                var duplicates =
                    await _repository.CheckClientDuplicatesAsync(
                        gst, mobile, email, dto.ClientID);

                if (duplicates.mobileExists)
                    return new ResponseModel(400, "Mobile number already exists");

                if (duplicates.emailExists)
                    return new ResponseModel(400, "Email already exists");

                if (duplicates.gstExists)
                    return new ResponseModel(400, "GST No already exists");

                oldClient.CompanyName = dto.CompanyName;
                oldClient.ContactPerson = dto.ContactPerson;
                oldClient.Mobile = mobile;
                oldClient.Email = email;
                oldClient.GSTNo = gst;
                oldClient.BillingAddress = dto.BillingAddress;
                oldClient.StateID = dto.StateID;
                oldClient.CityID = dto.CityID;
                oldClient.ClientType = dto.ClientType;
                oldClient.Status = dto.Status;
                oldClient.Notes = dto.Notes;
                oldClient.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(dto);

                var updatedClient = await _repository.GetByIdAsync(dto.ClientID);

                return CommonHelper.SuccessResponseMessage(
                    "Client updated successfully",
                    updatedClient);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<ResponseModel> DeleteAsync(Guid id, string deletedBy)
        {
            try
            {
                var deleted = await _repository.DeleteAsync(id, deletedBy);

                if (!deleted)
                    return new ResponseModel(404, "Client not found");

                return CommonHelper.SuccessResponseMessage(
                    "Client deleted successfully",
                    null);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

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
    }
}