using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.User
{
    public interface IUserManagementService
    {
        Task<ResponseModel> CreateUserAsync(CreateUserRequestModel request, string createdByUserId);
        Task<ResponseModel> UpdateUserAsync(UpdateUserRequestModel request);
        Task<ResponseModel> AssignPermissionsAsync(AssignPermissionsRequestModel request,string grantedByUserId);
        Task<ResponseModel> GetMyPermissionsAsync(string userId);
        Task<ResponseModel> GetMenuAsync(string userId);
    }

}
