using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth
{
    public interface IAuthService
    {
        Task<ResponseModel> RegisterUser(UserRegisterRequestModel request);
        Task<ResponseModel> Login(UserLoginRequestModel model);
    }
}
