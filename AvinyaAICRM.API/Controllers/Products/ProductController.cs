using AvinyaAICRM.Application.DTOs.Product;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Products;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.Api.Controllers.Products
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService Service)
        {
            _service = Service;
        }

        [HttpGet("get-Product-dropdown")]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _service.GetAllAsync(userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _service.GetByIdAsync(id);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductRequest product)
        {
            var userId = User.FindFirst("userId")?.Value!;
            product.CreatedBy = userId;
            var response = await _service.CreateAsync(product);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }


        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductDto productDto)
        {
            if (productDto == null)
                return BadRequest("product data is required.");

            productDto.ProductID = id;

            var response = await _service.UpdateAsync(productDto);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _service.DeleteAsync(id, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered(string? search = null, bool? status = null, int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _service.GetFilteredAsync(search, status, page, pageSize, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }


        [HttpGet("get-UnitType-dropdown")]
        public async Task<IActionResult> GetUnitType()
        {
            var response = await _service.GetUnitTypeAsync();
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }
    }
}
