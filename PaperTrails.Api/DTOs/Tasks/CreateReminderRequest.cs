namespace PaperTrails.Api.DTOs.Tasks
{
    public class CreateReminderRequest
    {
        public DateTime ReminderTime { get; set; }
        public string Recurrence { get; set; } // "None", "Weekly", "Monthly" - parse to enum
    }
}
