using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaperTrails.Api.Data;
using PaperTrails.Api.Models;
using PaperTrails.Api.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PaperTrails.Api.HostedServices
{
    public class ReminderHostedService : BackgroundService
    {
        private readonly ILogger<ReminderHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public ReminderHostedService(
            ILogger<ReminderHostedService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderHostedService started, interval {Interval}", _interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var cutoffUtc = DateTime.UtcNow;
                    var dueReminders = await dbContext.Reminders
                        .Include(r => r.TaskItem)
                        .Where(r =>
                            r.ReminderTime <= cutoffUtc &&
                            (r.LastFiredAt == null || r.LastFiredAt < r.ReminderTime) &&
                            !r.TaskItem.IsCompleted)
                        .ToListAsync(stoppingToken);

                    if (dueReminders.Any())
                    {
                        _logger.LogInformation("Found {Count} due reminders at {Time}", dueReminders.Count, cutoffUtc);
                    }

                    foreach (var reminder in dueReminders)
                    {
                        try
                        {
                            _logger.LogInformation(" Firing reminder for task '{Title}' (UserId: {UserId})",
                                reminder.TaskItem.Title, reminder.TaskItem.UserId);

                            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                            await notificationService.SendAsync(
                                reminder.TaskItem.UserId,
                                $"Reminder: {reminder.TaskItem.Title}",
                                $"Your task \"{reminder.TaskItem.Title}\" is due soon.");


                            reminder.LastFiredAt = DateTime.UtcNow;

                            switch (reminder.Recurrence)
                            {
                                case RecurrenceType.Weekly:
                                    reminder.ReminderTime = reminder.ReminderTime.AddDays(7);
                                    _logger.LogInformation("Rescheduled weekly reminder for {NextTime}", reminder.ReminderTime);
                                    break;

                                case RecurrenceType.Monthly:
                                    reminder.ReminderTime = reminder.ReminderTime.AddMonths(1);
                                    _logger.LogInformation("Rescheduled monthly reminder for {NextTime}", reminder.ReminderTime);
                                    break;

                                case RecurrenceType.None:
                                default:
                                    _logger.LogInformation("One-time reminder completed for task '{Title}'", reminder.TaskItem.Title);
                                    break;
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing reminder {Id}", reminder.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ReminderHostedService iteration failed");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("ReminderHostedService stopped.");
        }
    }
}
