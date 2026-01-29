using AvinyaAICRM.Application.DTOs.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tasks
{
    public interface ITaskRepository
    {
        Task<long> CreateTaskAsync(CreateTaskDto dto, string userId);
        Task<List<TaskDto>> GetTasksAsync(string userId, DateTime? from, DateTime? to);
        Task<bool> UpdateTaskAsync(long occurrenceId, UpdateTaskDto dto);
        Task<bool> DeleteTaskAsync(long occurrenceId);
        Task<bool> UpdateRecurringAsync(long taskSeriesId, UpdateRecurringDto dto);
        Task<bool> AddReminderAsync(long occurrenceId, CreateReminderDto dto);
        Task<bool> UpdateReminderAsync(long reminderId, CreateReminderDto dto);
        Task<bool> DeleteReminderAsync(long reminderId);
        Task<TaskDetailsDto?> GetTaskDetailsAsync(long occurrenceId, string userId);
    }

}
