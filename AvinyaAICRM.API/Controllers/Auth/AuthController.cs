using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterUser([FromBody] UserRegisterRequestModel request)
        {
            var result = await _authService.RegisterUser(request);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("login")]
        public async Task<ActionResult> LoginUser([FromBody] UserLoginRequestModel request)
        {
            var result = await _authService.Login(request);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
