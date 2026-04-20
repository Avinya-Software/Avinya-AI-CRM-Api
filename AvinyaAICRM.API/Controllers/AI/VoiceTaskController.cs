using AvinyaAICRM.Application.DTOs.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.AI
{
    [ApiController]
    [Authorize]
    [Route("api/voice-task")]
    public class VoiceTaskController : ControllerBase
    {
        private readonly IIntentService _intentService;
        private readonly ITaskService _taskService;

        public VoiceTaskController(
            IIntentService intentService,
            ITaskService taskService)
        {
            _intentService = intentService;
            _taskService   = taskService;
        }

        /// <summary>
        /// Creates a task from a voice/text command.
        /// The text is parsed for date ("aaj", "kal", "parso", "somwar", "5 baje", etc.),
        /// time period (subah / sham / raat / dopahar), recurrence, reminders, status, and assignee.
        /// </summary>
        /// <param name="dto">Voice input payload containing the raw text string.</param>
        [HttpPost("add")]
        public async Task<IActionResult> Create([FromBody] VoiceInputDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest(new { message = "Voice text cannot be empty." });

            var (intent, confidence) = _intentService.Predict(dto.Text);

            var userId = User.FindFirst("userId")!.Value;

            var response = await _taskService.CreateTaskUsingVoiceAsync(userId, intent, dto.Text);

            return new JsonResult(response) { StatusCode = response.StatusCode };
        }
    }
}
