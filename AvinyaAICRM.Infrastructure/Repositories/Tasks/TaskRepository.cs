using AvinyaAICRM.Application.DTOs.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tasks;
using AvinyaAICRM.Domain.Entities.Tasks;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

                // 🧠 Normalize dates
                var dueDateUtc = dto.DueDateTime;
                var recurrenceStartUtc = dto.RecurrenceStartDate?.ToUniversalTime();
                var recurrenceEndUtc = dto.RecurrenceEndDate?.ToUniversalTime(); // NULL = Never Ends

                // 1️⃣ Create TaskSeries
                var series = new TaskSeries
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Notes = dto.Notes,
                    ListId = listId,

                    IsRecurring = dto.IsRecurring,
                    RecurrenceRule = dto.IsRecurring ? dto.RecurrenceRule : null,

                    // If recurring → use recurrence start
                    // Else → keep null (single task)
                    StartDate = dto.IsRecurring
                        ? (recurrenceStartUtc ?? dueDateUtc)
                        : null,

                    // NULL means "Never Ends"
                    EndDate = dto.IsRecurring ? recurrenceEndUtc : null,

                    CreatedBy = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskSeries.Add(series);
                await _context.SaveChangesAsync();

                // 2️⃣ Create FIRST TaskOccurrence
                var occurrence = new TaskOccurrence
                {
                    TaskSeriesId = series.Id,
                    DueDateTime = dueDateUtc,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskOccurrences.Add(occurrence);
                await _context.SaveChangesAsync();

                // 3️⃣ Create Reminder (NotificationRules)
                if (dto.ReminderAt.HasValue && dueDateUtc.HasValue)
                {
                    var reminderUtc = dto.ReminderAt.Value.ToUniversalTime();

                   // Safety check
                    if (reminderUtc < dueDateUtc.Value)
                    {
                        var offsetMinutes =
                            (int)(dueDateUtc.Value - reminderUtc).TotalMinutes;

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



        public async Task<List<TaskDto>> GetTasksAsync(string userId, DateTime? from, DateTime? to)
        {
            var query = _context.TaskOccurrences
                .Include(x => x.TaskSeries)
                .Where(x => x.TaskSeries.CreatedBy == userId);

            if (from.HasValue && to.HasValue)
            {
                var fromDate = from.Value.Date;
                var toDate = to.Value.Date.AddDays(1).AddTicks(-1);

                query = query.Where(x =>
                    x.DueDateTime >= fromDate &&
                    x.DueDateTime <= toDate
                );
            }

            return await query
                .OrderBy(x => x.DueDateTime)
                .Select(x => new TaskDto
                {
                    OccurrenceId = x.Id,
                    Title = x.TaskSeries.Title,
                    DueDateTime = x.DueDateTime,
                    Status = x.Status,
                    IsRecurring = x.TaskSeries.IsRecurring
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateTaskAsync(long occurrenceId, UpdateTaskDto dto)
        {
            try
            {
                var task = await _context.TaskOccurrences.FindAsync(occurrenceId);
                if (task == null) return false;

                task.DueDateTime = dto.DueDateTime;
                task.Status = dto.Status ?? task.Status;

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
