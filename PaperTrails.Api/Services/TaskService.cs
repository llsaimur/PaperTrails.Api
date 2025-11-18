using Microsoft.EntityFrameworkCore;
using PaperTrails.Api.Data;
using PaperTrails.Api.DTOs.Tasks;
using PaperTrails.Api.Models;

namespace PaperTrails.Api.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _db;

        public TaskService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<TaskResponse> CreateTaskAsync(string userId, CreateTaskRequest dto)
        {
            // if DocumentId provided, ensure document exists and belongs to user
            if (!string.IsNullOrEmpty(dto.DocumentId))
            {
                var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == dto.DocumentId && d.UserId == userId);
                if (doc == null)
                    throw new InvalidOperationException("Document not found or does not belong to user.");
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                DocumentId = dto.DocumentId,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return MapToResponse(task);
        }

        public async Task<TaskResponse?> GetTaskAsync(string userId, string taskId)
        {
            var task = await _db.Tasks
                .Include(t => t.Reminders)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null) return null;
            return MapToResponse(task);
        }

        public async Task<(IEnumerable<TaskResponse> tasks, int total)> GetTasksAsync(string userId, int page = 1, int limit = 50, bool overdueOnly = false, bool? completed = null)
        {
            var query = _db.Tasks.Where(t => t.UserId == userId);

            if (overdueOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate < now);
            }

            if (completed.HasValue)
                query = query.Where(t => t.IsCompleted == completed.Value);

            var total = await query.CountAsync();

            var tasks = await query
                .OrderBy(t => t.IsCompleted) // incomplete first
                .ThenBy(t => t.DueDate)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(t => t.Reminders)
                .ToListAsync();

            var mapped = tasks.Select(MapToResponse).ToList();
            return (mapped, total);
        }

        public async Task<TaskResponse?> UpdateTaskAsync(string userId, string taskId, UpdateTaskRequest dto)
        {
            var task = await _db.Tasks.Include(t => t.Reminders).FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Title)) task.Title = dto.Title;
            if (dto.Description != null) task.Description = dto.Description;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
            if (dto.IsCompleted.HasValue) task.IsCompleted = dto.IsCompleted.Value;

            task.UpdatedAt = DateTime.UtcNow;

            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();

            return MapToResponse(task);
        }

        public async Task<bool> DeleteTaskAsync(string userId, string taskId)
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null) return false;

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkCompleteAsync(string userId, string taskId, bool complete)
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            task.IsCompleted = complete;
            task.UpdatedAt = DateTime.UtcNow;

            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();

            return true;
        }


        public async Task<ReminderDto> AddReminderAsync(string userId, string taskId, CreateReminderRequest dto)
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null) throw new InvalidOperationException("Task not found or access denied.");

            if (dto.ReminderTime.Kind == DateTimeKind.Unspecified)
                dto.ReminderTime = DateTime.SpecifyKind(dto.ReminderTime, DateTimeKind.Utc);

            RecurrenceType recurrence = RecurrenceType.None;
            if (!string.IsNullOrEmpty(dto.Recurrence) && Enum.TryParse<RecurrenceType>(dto.Recurrence, true, out var parsed))
                recurrence = parsed;

            var reminder = new Reminder
            {
                Id = Guid.NewGuid().ToString(),
                TaskItemId = task.Id,
                ReminderTime = dto.ReminderTime.ToUniversalTime(),
                Recurrence = recurrence,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reminders.Add(reminder);
            await _db.SaveChangesAsync();

            // TODO: schedule push/local notification (Hangfire/Quartz integration) - future step
            return new ReminderDto { Id = reminder.Id, ReminderTime = reminder.ReminderTime, Recurrence = reminder.Recurrence.ToString() };
        }

        public async Task<IEnumerable<ReminderDto>> GetRemindersForUserAsync(string userId)
        {
            var reminders = await _db.Reminders
                .Include(r => r.TaskItem)
                .Where(r => r.TaskItem.UserId == userId)
                .ToListAsync();

            return reminders.Select(r => new ReminderDto { Id = r.Id, ReminderTime = r.ReminderTime, Recurrence = r.Recurrence.ToString() });
        }

        private TaskResponse MapToResponse(TaskItem t)
        {
            return new TaskResponse
            {
                Id = t.Id,
                DocumentId = t.DocumentId,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Reminders = t.Reminders?.Select(r => new ReminderDto
                {
                    Id = r.Id,
                    ReminderTime = r.ReminderTime,
                    Recurrence = r.Recurrence.ToString()
                }) ?? Enumerable.Empty<ReminderDto>()
            };
        }

        public async Task<ReminderDto?> UpdateReminderAsync(string userId, string taskId, string reminderId, UpdateReminderRequest dto)
        {
            var reminder = await _db.Reminders
                .Include(r => r.TaskItem)
                .FirstOrDefaultAsync(r => r.Id == reminderId && r.TaskItemId == taskId && r.TaskItem.UserId == userId);

            if (reminder == null) return null;

            if (dto.ReminderTime.Kind == DateTimeKind.Unspecified)
                dto.ReminderTime = DateTime.SpecifyKind(dto.ReminderTime, DateTimeKind.Utc);

            reminder.ReminderTime = dto.ReminderTime.ToUniversalTime();

            if (!string.IsNullOrEmpty(dto.Recurrence) && Enum.TryParse<RecurrenceType>(dto.Recurrence, true, out var parsed))
                reminder.Recurrence = parsed;


            _db.Reminders.Update(reminder);
            await _db.SaveChangesAsync();

            // TODO: reschedule notification if needed
            return new ReminderDto
            {
                Id = reminder.Id,
                ReminderTime = reminder.ReminderTime,
                Recurrence = reminder.Recurrence.ToString()
            };
        }

        public async Task<bool> DeleteReminderAsync(string userId, string taskId, string reminderId)
        {
            var reminder = await _db.Reminders
                .Include(r => r.TaskItem)
                .FirstOrDefaultAsync(r => r.Id == reminderId && r.TaskItemId == taskId && r.TaskItem.UserId == userId);

            if (reminder == null) return false;

            _db.Reminders.Remove(reminder);
            await _db.SaveChangesAsync();

            // TODO: cancel scheduled notification if needed
            return true;
        }

    }
}
