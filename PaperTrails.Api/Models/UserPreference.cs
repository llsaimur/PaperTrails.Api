using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PaperTrails.Api.Models
{
    public class UserPreference
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        // Daily digest time — store as TimeSpan in DB or as string; use TimeSpan here
        public TimeSpan DailyDigestTime { get; set; } = new TimeSpan(8, 0, 0); // 08:00 by default

        public bool EnablePushNotifications { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
