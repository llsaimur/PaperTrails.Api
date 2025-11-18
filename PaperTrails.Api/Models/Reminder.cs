using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PaperTrails.Api.Models
{
    public enum RecurrenceType
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }

    public class Reminder
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TaskItemId { get; set; }

        [ForeignKey(nameof(TaskItemId))]
        public TaskItem TaskItem { get; set; }

        [Required]
        public DateTime ReminderTime { get; set; }

        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastFiredAt { get; set; } = null;
    }
}
