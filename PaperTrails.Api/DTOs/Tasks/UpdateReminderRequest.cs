namespace PaperTrails.Api.DTOs.Tasks
{
    public class UpdateReminderRequest
    {
        public DateTime ReminderTime { get; set; }
        public string Recurrence { get; set; } = "none";
    }

}
