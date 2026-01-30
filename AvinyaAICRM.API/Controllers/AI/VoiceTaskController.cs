using AvinyaAICRM.Application.DTOs.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.AI
{
    [ApiController]
    [Route("api/voice-task")]
    public class VoiceTaskController : ControllerBase
    {
        private readonly IIntentService _intentService;
        private readonly ITaskService _taskService; // 👈 EXISTING SERVICE

        public VoiceTaskController(
            IIntentService intentService,
            ITaskService taskService)
        {
            _intentService = intentService;
            _taskService = taskService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Create([FromBody] VoiceInputDto dto)
        {
            var (intent, confidence) = _intentService.Predict(dto.Text);

            var userId = User.FindFirst("userId")!.Value;

            var response = await _taskService.CreateTaskUsingVoiceAsync(userId, intent, dto.Text);

            return new JsonResult(response) { StatusCode = response.StatusCode };
        }

    }


}
