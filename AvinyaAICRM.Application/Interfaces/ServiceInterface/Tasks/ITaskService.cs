using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks
{
    public interface ITaskService
    {
        Task<ResponseModel> CreateTaskAsync(CreateTaskDto dto, string userId);
        Task<ResponseModel> GetTasksAsync(string userId, DateTime? from, DateTime? to);
        Task<ResponseModel> UpdateTaskAsync(long occurrenceId, UpdateTaskDto dto);
        Task<ResponseModel> DeleteTaskAsync(long occurrenceId);
        Task<ResponseModel> UpdateRecurringAsync(long taskSeriesId, UpdateRecurringDto dto);
        Task<ResponseModel> AddReminderAsync(long occurrenceId, CreateReminderDto dto);
        Task<ResponseModel> UpdateReminderAsync(long occurrenceId, CreateReminderDto dto);
        Task<ResponseModel> DeleteReminderAsync(long occurrenceId);
        Task<ResponseModel> GetTaskDetailsAsync(long occurrenceId, string userId);

    }

}
