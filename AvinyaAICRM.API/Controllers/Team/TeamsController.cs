using AvinyaAICRM.Application.DTOs.Team;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Team
{    
    [ApiController]
    [Route("api/teams")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _service;

        public TeamsController(ITeamService service)
        {
            _service = service;
        }

        private string CurrentUserId =>
            User.FindFirst("userId")?.Value!;

        [Authorize]
        [HttpGet("get")]
        public async Task<IActionResult> GetMyManaged()
        {
            var result = await _service.GetMyManagedTeamsAsync(CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateTeamDto dto)
        {
            var result = await _service.CreateAsync(dto, CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(long id, UpdateTeamDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id, CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpGet("dropdown")]
        public async Task<IActionResult> GetDropdown()
        {
            var result = await _service.GetDropdownAsync(CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }

}
