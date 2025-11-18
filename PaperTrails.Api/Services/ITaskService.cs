using PaperTrails.Api.DTOs.Tasks;

namespace PaperTrails.Api.Services
{
    public interface ITaskService
    {
        Task<TaskResponse> CreateTaskAsync(string userId, CreateTaskRequest dto);
        Task<TaskResponse?> GetTaskAsync(string userId, string taskId);
        Task<(IEnumerable<TaskResponse> tasks, int total)> GetTasksAsync(string userId, int page = 1, int limit = 50, bool overdueOnly = false, bool? completed = null);
        Task<TaskResponse?> UpdateTaskAsync(string userId, string taskId, UpdateTaskRequest dto);
        Task<bool> DeleteTaskAsync(string userId, string taskId);
        Task<bool> MarkCompleteAsync(string userId, string taskId, bool complete);

        Task<ReminderDto> AddReminderAsync(string userId, string taskId, CreateReminderRequest dto);
        Task<ReminderDto?> UpdateReminderAsync(string userId, string taskId, string reminderId, UpdateReminderRequest dto);
        Task<bool> DeleteReminderAsync(string userId, string taskId, string reminderId);
        Task<IEnumerable<ReminderDto>> GetRemindersForUserAsync(string userId);
    }
}
