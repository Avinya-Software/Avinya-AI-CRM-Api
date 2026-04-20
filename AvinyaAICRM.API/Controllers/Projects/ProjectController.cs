using Microsoft.AspNetCore.Mvc;
using AvinyaAICRM.Application.DTOs.Projects;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Projects;
using Microsoft.AspNetCore.Authorization;

namespace AvinyaAICRM.API.Controllers.Projects
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var response = await _projectService.GetAllAsync(tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetAllFilter(string? search,
         int? statusFilter,
         DateTime? startDate,
         DateTime? endDate,
         int pageNumber,
         int pageSize)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _projectService.GetAllFilter(search, statusFilter, startDate, endDate, pageNumber, pageSize, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var response = await _projectService.GetByIdAsync(id, tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateUpdateDto dto)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var userId = User.FindFirst("userId")?.Value!;
            var response = await _projectService.CreateAsync(dto, tenantId, userId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProjectCreateUpdateDto dto)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var response = await _projectService.UpdateAsync(dto, tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = User.FindFirst("tenantId")?.Value!;
            var response = await _projectService.DeleteAsync(id, tenantId);
            return new JsonResult(response) { StatusCode = response.StatusCode };
        }
    }
}
