using AvinyaAICRM.Application.Interfaces.ServiceInterface.TaxCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaxCategoryController : ControllerBase
    {
        private readonly ITaxCategoryService _Service;

        public TaxCategoryController(ITaxCategoryService Service)
        {
            _Service = Service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _Service.GetAllAsync();
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _Service.GetByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
