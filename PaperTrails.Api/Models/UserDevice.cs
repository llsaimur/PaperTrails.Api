namespace PaperTrails.Api.Models
{
    public class UserDevice
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;

        public string DeviceToken { get; set; } = string.Empty; // FCM/APNs token

        public string Platform { get; set; } = "iOS"; // or "Android"

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
