namespace PaperTrails.Api.DTOs.Tasks
{
    public class TaskResponse
    {
        public string Id { get; set; }
        public string? DocumentId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<ReminderDto> Reminders { get; set; } = Enumerable.Empty<ReminderDto>();
    }

    public class ReminderDto
    {
        public string Id { get; set; }
        public DateTime ReminderTime { get; set; }
        public string Recurrence { get; set; }
    }
}
