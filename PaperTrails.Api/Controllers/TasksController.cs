using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PaperTrails.Api.DTOs.Tasks;
using PaperTrails.Api.Services;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] bool overdueOnly = false, [FromQuery] bool? completed = null)
    {
        var userId = GetUserId();
        var (tasks, total) = await _taskService.GetTasksAsync(userId, page, limit, overdueOnly, completed);
        return Ok(new { page, limit, total, data = tasks });
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        var userId = GetUserId();
        var (tasks, total) = await _taskService.GetTasksAsync(userId, 1, 1000, overdueOnly: true);
        return Ok(new { total, data = tasks });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(string id)
    {
        var userId = GetUserId();
        var task = await _taskService.GetTaskAsync(userId, id);
        if (task == null) return NotFound(new { error = "Task not found" });
        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { error = "Title is required" });

        try
        {
            var result = await _taskService.CreateTaskAsync(userId, request);
            return CreatedAtAction(nameof(GetTask), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] UpdateTaskRequest request)
    {
        var userId = GetUserId();
        var updated = await _taskService.UpdateTaskAsync(userId, id, request);
        if (updated == null) return NotFound(new { error = "Task not found" });
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(string id)
    {
        var userId = GetUserId();
        var ok = await _taskService.DeleteTaskAsync(userId, id);
        if (!ok) return NotFound(new { error = "Task not found" });
        return Ok(new { message = "Task deleted" });
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> MarkComplete(string id, [FromQuery] bool? complete = null)
    {
        if (complete == null)
            return BadRequest(new { error = "Missing 'complete' parameter (true/false required)" });

        var userId = GetUserId();
        var ok = await _taskService.MarkCompleteAsync(userId, id, complete.Value);

        if (!ok)
            return NotFound(new { error = "Task not found" });

        var message = complete.Value ? "Task marked complete" : "Task marked incomplete";
        return Ok(new { message });
    }



    [HttpPost("{id}/reminders")]
    public async Task<IActionResult> AddReminder(string id, [FromBody] CreateReminderRequest request)
    {
        var userId = GetUserId();
        try
        {
            var reminder = await _taskService.AddReminderAsync(userId, id, request);
            return CreatedAtAction(nameof(GetTask), new { id = id }, reminder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("reminders")]
    public async Task<IActionResult> GetReminders()
    {
        var userId = GetUserId();
        var reminders = await _taskService.GetRemindersForUserAsync(userId);
        return Ok(reminders);
    }

    [HttpDelete("{taskId}/reminders/{reminderId}")]
    public async Task<IActionResult> DeleteReminder(string taskId, string reminderId)
    {
        var userId = GetUserId();
        var ok = await _taskService.DeleteReminderAsync(userId, taskId, reminderId);

        if (!ok) return NotFound(new { error = "Reminder not found" });
        return Ok(new { message = "Reminder deleted" });
    }

    [HttpPut("{taskId}/reminders/{reminderId}")]
    public async Task<IActionResult> UpdateReminder(string taskId, string reminderId, [FromBody] UpdateReminderRequest request)
    {
        var userId = GetUserId();
        try
        {
            var updated = await _taskService.UpdateReminderAsync(userId, taskId, reminderId, request);
            if (updated == null) return NotFound(new { error = "Reminder not found" });
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}
