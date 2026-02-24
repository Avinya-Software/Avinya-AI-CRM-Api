using AvinyaAICRM.Application.DTOs.Client;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace AvinyaAICRM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _service;

        public ClientController(IClientService service)
        {
            _service = service;
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered(string? search = null, bool? status = null, int page = 1, int pageSize = 10)
        {
            var result = await _service.GetFilteredAsync(search, status, page, pageSize);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpGet("get-user-dropdown-list")]
        public async Task<IActionResult> GetAll([FromQuery] bool getAll = false)
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
        public async Task<IActionResult> Create([FromBody] ClientRequestDto client)
        {
            var result = await _service.CreateAsync(client);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ClientRequestDto client)
        {
            client.ClientID = id; 
            var result = await _service.UpdateAsync(client);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("Session expired. Please login again.");
            }

            var result = await _service.DeleteAsync(id, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }
}
