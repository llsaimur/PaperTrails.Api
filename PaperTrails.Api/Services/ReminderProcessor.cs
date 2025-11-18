using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaperTrails.Api.Data;
using PaperTrails.Api.Models;

namespace PaperTrails.Api.Services
{
    public interface IPushNotificationService
    {
        Task SendReminderAsync(string userId, string taskId, string title, string body);
    }

    // Simple logger-backed default implementation will replace with FCM/APNs later
    public class LoggingPushNotificationService : IPushNotificationService
    {
        private readonly ILogger<LoggingPushNotificationService> _logger;
        public LoggingPushNotificationService(ILogger<LoggingPushNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendReminderAsync(string userId, string taskId, string title, string body)
        {
            _logger.LogInformation("SendReminder: userId={UserId} taskId={TaskId} title={Title} body={Body}",
                userId, taskId, title, body);
            // TODO: call FCM/APNs here
            return Task.CompletedTask;
        }
    }

    public class ReminderProcessor
    {
        private readonly AppDbContext _db;
        private readonly IPushNotificationService _push;
        private readonly ILogger<ReminderProcessor> _logger;

        public ReminderProcessor(AppDbContext db, IPushNotificationService push, ILogger<ReminderProcessor> logger)
        {
            _db = db;
            _push = push;
            _logger = logger;
        }

        /// <summary>
        /// Process due reminders up to `cutoffUtc`. Returns number processed.
        /// </summary>
        public async Task<int> ProcessDueRemindersAsync(DateTime cutoffUtc, CancellationToken ct = default)
        {
            // Find reminders whose ReminderTime is <= cutoff and that haven't been fired for that time.
            // We treat a reminder as "due" if LastFiredAt == null || LastFiredAt < ReminderTime
            var due = await _db.Reminders
                .Include(r => r.TaskItem)
                .Where(r => r.ReminderTime <= cutoffUtc
                            && (r.LastFiredAt == null || r.LastFiredAt < r.ReminderTime)
                            && r.TaskItem != null
                            && !r.TaskItem.IsCompleted)
                .ToListAsync(ct);

            if (!due.Any()) return 0;

            foreach (var r in due)
            {
                try
                {
                    var title = $"Reminder: {r.TaskItem.Title}";
                    var body = r.TaskItem.Description ?? $"Due {r.TaskItem.DueDate?.ToString("g") ?? "soon"}";

                    await _push.SendReminderAsync(r.TaskItem.UserId, r.TaskItem.Id, title, body);

                    r.LastFiredAt = DateTime.UtcNow;

                    if (r.Recurrence != RecurrenceType.None)
                    {
                        var next = RecurrenceHelper.NextOccurrence(r.ReminderTime, r.Recurrence);
                        while (next <= DateTime.UtcNow) next = RecurrenceHelper.Advance(next, r.Recurrence);

                        r.ReminderTime = next;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reminder {ReminderId}", r.Id);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Processed {Count} reminder(s)", due.Count);
            return due.Count;
        }
    }
}
