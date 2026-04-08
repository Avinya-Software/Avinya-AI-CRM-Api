using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using AvinyaAICRM.Application.Validators;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Tasks
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepo;
        private readonly ITeamRepository _teamRepo;
        private readonly IUserRepository _userRepo;

        public TaskService(ITaskRepository taskRepo, ITeamRepository teamRepo, IUserRepository userRepo)
        {
            _taskRepo = taskRepo;
            _teamRepo = teamRepo;
            _userRepo = userRepo;
        }

        public async Task<ResponseModel> CreateTaskAsync(CreateTaskDto dto, string userId)
        {
            var task = await _taskRepo.CreateTaskAsync(dto, userId);
            return CommonHelper.GetResponseMessage(task);
        }

        public async Task<ResponseModel> GetTasksAsync(string userId, DateTime? from, DateTime? to, string? scope)
        {
            var tasks = await _taskRepo.GetTasksAsync(userId, from, to, scope);
            return CommonHelper.GetResponseMessage(tasks);
        }

        public async Task<ResponseModel> UpdateTaskAsync(long occurrenceId, UpdateTaskDto dto)
        {
            var updated = await _taskRepo.UpdateTaskAsync(occurrenceId, dto);
            return CommonHelper.GetResponseMessage(updated);
        }

        public async Task<ResponseModel> DeleteTaskAsync(long occurrenceId)
        {
            var deleted = await _taskRepo.DeleteTaskAsync(occurrenceId);
            return CommonHelper.GetResponseMessage(deleted);
        }

        public async Task<ResponseModel> UpdateRecurringAsync(long taskSeriesId, UpdateRecurringDto dto)
        {
            var result = await _taskRepo.UpdateRecurringAsync(taskSeriesId, dto);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> AddReminderAsync(long occurrenceId, CreateReminderDto dto)
        {
            var result = await _taskRepo.AddReminderAsync(occurrenceId, dto);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> UpdateReminderAsync(long occurrenceId, CreateReminderDto dto)
        {
            var result = await _taskRepo.UpdateReminderAsync(occurrenceId, dto);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> DeleteReminderAsync(long occurrenceId)
        {
            var result = await _taskRepo.DeleteReminderAsync(occurrenceId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetTaskDetailsAsync(long occurrenceId,string userId)
        {
            var task = await _taskRepo.GetTaskDetailsAsync(occurrenceId, userId);
            return CommonHelper.GetResponseMessage(task);
        }

        public async Task<ResponseModel> CreateTaskUsingVoiceAsync(string userId, string intent, string text)
        {
            if (intent == "Unknown")
                return CommonHelper.BadRequestResponseMessage("Unable to understand");

            // Extract due date directly as UTC
            var dueDateUtc = VoiceDateTimeExtractor.ExtractDueDate(text);

            // Reminder resolution (assuming it returns local IST time)
            var reminderLocalIst = VoiceReminderResolver.ResolveReminder(text, dueDateUtc);

            DateTime? reminderUtc = null;

            if (reminderLocalIst.HasValue)
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

                var cleanReminder = DateTime.SpecifyKind(reminderLocalIst.Value, DateTimeKind.Unspecified);
                reminderUtc = TimeZoneInfo.ConvertTimeToUtc(cleanReminder, istZone);
            }

            var (isRecurring, rule) = VoiceRecurrenceParser.Parse(text);
            var entities = VoiceEntityExtractor.Extract(text);
            var status = VoiceStatusParser.ExtractStatus(text);

            string? assignToId = null;
            long? teamId = null;

            if (!string.IsNullOrEmpty(entities.AssigneeName))
            {
                var user = await _userRepo.GetUserName(entities.AssigneeName);
                assignToId = user?.Id;
            }

            if (entities.IsTeamTask)
            {
                teamId = await _teamRepo.ResolveTeamId(userId, entities.TeamName);
            }

            var taskDto = new CreateTaskDto
            {
                Title = text,
                Description = "Created via voice",
                ListId = 0,
                Status = status,
                DueDateTime = dueDateUtc,        // Already in UTC
                ReminderAt = reminderUtc,        // Already in UTC
                AssignToId = assignToId,
                TeamId = teamId,
                IsRecurring = isRecurring,
                RecurrenceRule = rule
                // Add other fields if needed (RecurrenceStartDate, etc.)
            };

            var taskId = await _taskRepo.CreateTaskAsync(taskDto, userId);

            return CommonHelper.GetResponseMessage(taskId);   // Assuming it returns the ID or model
        }

    }

}
