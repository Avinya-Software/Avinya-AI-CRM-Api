using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Task
{
    
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _service;

        public TasksController(ITaskService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _service.CreateTaskAsync(dto, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpGet("get")]
        public async Task<IActionResult> GetTasks(DateTime? from, DateTime? to)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _service.GetTasksAsync(userId, from, to);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPut("{occurrenceId}")]
        public async Task<IActionResult> UpdateTask(long occurrenceId, UpdateTaskDto dto)
        {
            var result = await _service.UpdateTaskAsync(occurrenceId, dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpDelete("{occurrenceId}")]
        public async Task<IActionResult> DeleteTask(long occurrenceId)
        {
            var result = await _service.DeleteTaskAsync(occurrenceId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPut("series/{taskSeriesId}/recurring")]
        public async Task<IActionResult> UpdateRecurring(long taskSeriesId, UpdateRecurringDto dto)
        {
            var result = await _service.UpdateRecurringAsync(taskSeriesId, dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPost("add/{occurrenceId}/reminders")]
        public async Task<IActionResult> AddReminder(long occurrenceId, CreateReminderDto dto)
        {
            var result = await _service.AddReminderAsync(occurrenceId, dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpPost("update/{occurrenceId}/reminders")]
        public async Task<IActionResult> UpdateReminder(long occurrenceId, CreateReminderDto dto)
        {
            var result = await _service.UpdateReminderAsync(occurrenceId, dto);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpDelete("reminders/{reminderId}")]
        public async Task<IActionResult> DeleteReminder(long occurrenceId)
        {
            var result = await _service.DeleteReminderAsync(occurrenceId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }

        [Authorize]
        [HttpGet("get/{occurrenceId}")]
        public async Task<IActionResult> GetTaskDetails(long occurrenceId)
        {
            var userId = User.FindFirst("userId")?.Value!;
            var result = await _service.GetTaskDetailsAsync(occurrenceId, userId);
            return new JsonResult(result) { StatusCode = result.StatusCode };
        }


    }

}
