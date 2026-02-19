using AvinyaAICRM.Application.DTOs.TeamMember;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.TeamMember;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.TeamMembers
{
    
    [ApiController]
    [Route("api/team/members")]
    public class TeamMembersController : ControllerBase
    {
        private readonly ITeamMemberService _service;

        public TeamMembersController(ITeamMemberService service)
        {
            _service = service;
        }

        private string CurrentUserId =>
            User.FindFirst("userId")?.Value!;

        [Authorize]
        [HttpGet("get")]
        public async Task<IActionResult> Get([FromQuery] long teamId)
        {
            var result = await _service.GetMembersAsync(teamId, CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> Add(long teamId, AddTeamMemberDto dto)
        {
            var result = await _service.AddMemberAsync(teamId, dto, CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpDelete("remove/{memberId}")]
        public async Task<IActionResult> Remove(long teamId, Guid memberId)
        {
            var result = await _service.RemoveMemberAsync(teamId, memberId, CurrentUserId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }
    }

}
