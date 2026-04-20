using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.DTOs.User;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.User
{
    [ApiController]
    [Route("api/users/credits")]
    public class UserCreditController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public UserCreditController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            var res = await _creditService.GetByUserIdAsync(userId);
            return new JsonResult(res) { StatusCode = res.StatusCode };
        }

        [HttpPut("balance/{userId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateBalance(string userId, [FromBody] int newBalance)
        {
            var res = await _creditService.UpdateBalanceAsync(userId, newBalance, "AdminUpdate", "Balance updated by admin");
            return new JsonResult(res) { StatusCode = res.StatusCode };
        }

        [HttpPost("list")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetList([FromBody] UserCreditFilterRequest request)
        {
            var res = await _creditService.GetUserCreditsAsync(request);
            return new JsonResult(res) { StatusCode = res.StatusCode };
        }

        [HttpGet("transactions/{userId}")]
        public async Task<IActionResult> GetTransactions(string userId, int pageNumber = 1, int pageSize = 20)
        {
            var res = await _creditService.GetTransactionsByUserIdAsync(userId, pageNumber, pageSize);
            return new JsonResult(res) { StatusCode = res.StatusCode };
        }

        [HttpPost("test-daily-reset")]
        public async Task<IActionResult> TestDailyReset()
        {
            await _creditService.ResetAllBalancesAsync(15000);
            return Ok(new { message = "Daily reset logic triggered successfully. All users updated to 15,000 tokens." });
        }
    }
}
