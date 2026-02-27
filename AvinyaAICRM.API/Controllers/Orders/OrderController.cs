using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvinyaAICRM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;
        private readonly IStatusDropDownServices _statusDropDownServices;

        public OrderController(IOrderService service, IStatusDropDownServices statusDropDownServices)
        {
            _service = service;
            _statusDropDownServices = statusDropDownServices;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var result = await _service.GetByIdAsync(id, tenantId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(string? search, int page = 1, int pageSize = 10, int? status = null, DateTime? from = null, DateTime? to = null)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _service.GetFilteredAsync(search, page, pageSize, userId, status, from, to);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] OrderDto dto)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _service.AddOrUpdateAsync(dto, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("get-OrderStatus-dropdown")]
        public async Task<IActionResult> GetOrderStatusDropDown()
        {
            var result = await _statusDropDownServices.GetAllOrderStatusAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("get-DesignStatus-dropdown")]
        public async Task<IActionResult> GetDesignStatusDropDown()
        {
            var result = await _statusDropDownServices.GetAllDesignStatusAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
