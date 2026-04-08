using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tasks;
using AvinyaAICRM.Domain.Entities.Tasks;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AvinyaAICRM.Infrastructure.Repositories.Tasks
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<long> CreateTaskAsync(CreateTaskDto dto, string userId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var listId = dto.ListId <= 0
                    ? await GetOrCreateDefaultListAsync(userId)
                    : dto.ListId;

                var series = new TaskSeries
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Notes = dto.Notes,
                    ListId = listId,

                    IsRecurring = dto.IsRecurring,
                    RecurrenceRule = dto.IsRecurring ? dto.RecurrenceRule : null,
                    StartDate = dto.IsRecurring
                        ? (dto.RecurrenceStartDate ?? dto.DueDateTime)
                        : null,

                    EndDate = dto.IsRecurring ? dto.RecurrenceEndDate : null,

                    TeamId = dto.TeamId,
                    TaskScope = dto.TeamId > 0 ? "Team" : "Personal",
                    CreatedBy = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ProjectId = !string.IsNullOrWhiteSpace(dto.ProjectId)
                        ? Guid.Parse(dto.ProjectId)
                        : null
                };

                _context.TaskSeries.Add(series);
                await _context.SaveChangesAsync();

                var occurrence = new TaskOccurrence
                {
                    TaskSeriesId = series.Id,
                    DueDateTime = dto.DueDateTime,                    // UTC
                    Status = string.IsNullOrEmpty(dto.Status) ? "Pending" : dto.Status,
                    CreatedAt = DateTime.UtcNow,
                    AssignedTo = dto.AssignToId
                };

                _context.TaskOccurrences.Add(occurrence);
                await _context.SaveChangesAsync();

                // Reminder handling
                if (dto.ReminderAt.HasValue && dto.DueDateTime.HasValue)
                {
                    var reminderUtc = dto.ReminderAt.Value;   // Already UTC

                    if (reminderUtc < dto.DueDateTime.Value)
                    {
                        var offsetMinutes = (int)(dto.DueDateTime.Value - reminderUtc).TotalMinutes;

                        var reminder = new NotificationRule
                        {
                            TaskOccurrenceId = occurrence.Id,
                            TriggerType = "BeforeDue",
                            OffsetMinutes = offsetMinutes,
                            Channel = dto.ReminderChannel ?? "InApp",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.NotificationRules.Add(reminder);
                        await _context.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
                return occurrence.Id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<List<TaskDto>> GetTasksAsync(
     string userId,
     DateTime? from,
     DateTime? to,
     string? scope)
        {
            var userGuid = Guid.Parse(userId);

            var query = _context.TaskOccurrences
                .Include(x => x.TaskSeries)
                .Where(x =>
                    x.TaskSeries.CreatedBy == userId
                    || x.AssignedTo == userId
                    || (
                        x.TaskSeries.TeamId != null &&
                        _context.TeamMembers.Any(tm =>
                            tm.TeamId == x.TaskSeries.TeamId &&
                            tm.UserId == userGuid
                        )
                    )
                );

            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            // =========================
            // ✅ FIX: FILTER (IMPORTANT)
            // =========================
            if (from.HasValue && to.HasValue)
            {
                // 🔥 FORCE Unspecified (CRITICAL FIX)
                var fromUnspecified = DateTime.SpecifyKind(from.Value, DateTimeKind.Unspecified);
                var toUnspecified = DateTime.SpecifyKind(to.Value, DateTimeKind.Unspecified);

                var fromUtc = TimeZoneInfo.ConvertTimeToUtc(fromUnspecified, istTimeZone);
                var toUtc = TimeZoneInfo.ConvertTimeToUtc(toUnspecified, istTimeZone);

                query = query.Where(x =>
                    x.DueDateTime >= fromUtc &&
                    x.DueDateTime <= toUtc);
            }

            var data = await query
                .OrderBy(x => x.DueDateTime)
                .Select(x => new TaskDto
                {
                    OccurrenceId = x.Id,
                    Title = x.TaskSeries.Title,
                    TeamId = x.TaskSeries.TeamId,
                    DueDateTime = x.DueDateTime, // stored UTC
                    Status = x.Status,
                    IsRecurring = x.TaskSeries.IsRecurring,
                    AssignedTo = x.AssignedTo
                })
                .ToListAsync();

            // =========================
            // ✅ FIX: DISPLAY
            // =========================
            foreach (var item in data)
            {
                if (item.DueDateTime.HasValue)
                {
                    item.DueDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                        item.DueDateTime.Value,
                        istTimeZone
                    );
                }
            }

            return data;
        }

        public async Task<bool> UpdateTaskAsync(long occurrenceId, UpdateTaskDto dto)
        {
            try
            {
                var task = await _context.TaskOccurrences.FindAsync(occurrenceId);
                var taskSeries = await _context.TaskSeries.FindAsync(task.TaskSeriesId);
                if (task == null) return false;

                task.DueDateTime = dto.DueDateTime;
                task.Status = dto.Status ?? task.Status;
                if (dto.AssignToId is not null)
                {
                  task.AssignedTo = dto.AssignToId;
                }

                taskSeries.TeamId = dto.TeamId;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        public async Task<bool> DeleteTaskAsync(long occurrenceId)
        {
            var task = await _context.TaskOccurrences.FindAsync(occurrenceId);
            if (task == null) return false;

            _context.TaskOccurrences.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateRecurringAsync(long taskSeriesId, UpdateRecurringDto dto)
        {
            var series = await _context.TaskSeries.FindAsync(taskSeriesId);
            if (series == null) return false;

            // End current series
            series.EndDate = DateTime.UtcNow;
            series.IsActive = false;

            // Create new series (future)
            var newSeries = new TaskSeries
            {
                Title = series.Title,
                Description = series.Description,
                Notes = series.Notes,
                ListId = series.ListId,
                IsRecurring = true,
                RecurrenceRule = dto.RecurrenceRule,
                StartDate = DateTime.UtcNow,
                EndDate = dto.EndDate,
                CreatedBy = series.CreatedBy
            };

            _context.TaskSeries.Add(newSeries);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddReminderAsync(long occurrenceId, CreateReminderDto dto)
        {
            var exists = await _context.TaskOccurrences
                .AnyAsync(x => x.Id == occurrenceId);

            if (!exists) return false;

            _context.NotificationRules.Add(new NotificationRule
            {
                TaskOccurrenceId = occurrenceId,
                TriggerType = dto.TriggerType,
                OffsetMinutes = dto.OffsetMinutes,
                Channel = dto.Channel
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateReminderAsync(long reminderId, CreateReminderDto dto)
        {
            var reminder = await _context.NotificationRules.FindAsync(reminderId);
            if (reminder == null) return false;

            reminder.TriggerType = dto.TriggerType;
            reminder.OffsetMinutes = dto.OffsetMinutes;
            reminder.Channel = dto.Channel;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReminderAsync(long reminderId)
        {
            var reminder = await _context.NotificationRules.FindAsync(reminderId);
            if (reminder == null) return false;

            _context.NotificationRules.Remove(reminder);
            await _context.SaveChangesAsync();
            return true;
        }


        private async Task<long> GetOrCreateDefaultListAsync(string userId)
        {
            var list = await _context.TaskLists
                .FirstOrDefaultAsync(x => x.OwnerId == Guid.Parse(userId)); 

            if (list != null)
                return list.Id;

            var newList = new TaskList
            {
                Name = "Personal",
                OwnerId = Guid.Parse(userId)
            };

            _context.TaskLists.Add(newList);
            await _context.SaveChangesAsync();

            return newList.Id;
        }

        public async Task<TaskDetailsDto?> GetTaskDetailsAsync(long occurrenceId,string userId)
        {
            var task = await _context.TaskOccurrences
                .Include(x => x.TaskSeries)
                .ThenInclude(s => s.List)
                .Where(x =>
                    x.Id == occurrenceId &&
                    x.TaskSeries.CreatedBy == userId)
                .Select(x => new TaskDetailsDto
                {
                    OccurrenceId = x.Id,
                    Title = x.TaskSeries.Title,
                    Description = x.TaskSeries.Description,
                    Notes = x.TaskSeries.Notes,

                    DueDateTime = x.DueDateTime,
                    StartDateTime = x.StartDateTime,
                    EndDateTime = x.EndDateTime,
                    Status = x.Status,

                    IsRecurring = x.TaskSeries.IsRecurring,
                    RecurrenceRule = x.TaskSeries.RecurrenceRule,

                    ListId = x.TaskSeries.ListId,
                    ListName = x.TaskSeries.List.Name
                })
                .FirstOrDefaultAsync();

            return task;
        }



    }

}
