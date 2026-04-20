using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.Api.Controllers.Orders
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderItemController : ControllerBase
    {
        private readonly IOrderItemService _service;

        public OrderItemController(IOrderItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderItemDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OrderItemDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetFilteredAsync(search, page, pageSize);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }

}