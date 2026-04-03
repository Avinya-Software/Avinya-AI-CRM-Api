using AvinyaAICRM.Application.DTOs.Expense;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Expense;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Expense
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(
     string? search,
     int page = 1,
     int pageSize = 10,
     string? status = null,
     DateTime? from = null,
     DateTime? to = null)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var result = await _expenseService.GetFilteredAsync(
                search,
                page,
                pageSize,
                tenantId,
                status,
                from,
                to);

            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateExpenseDto dto)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var userId = Guid.Parse(User.FindFirst("userId")?.Value!);

            var response = await _expenseService.CreateAsync(dto, tenantId, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromForm] UpdateExpenseDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("userId")?.Value!);
            var response = await _expenseService.UpdateAsync(dto, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _expenseService.DeleteAsync(id);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }
    }
}
