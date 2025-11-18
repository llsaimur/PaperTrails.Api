namespace PaperTrails.Api.Services
{
    public interface INotificationService
    {
        Task SendAsync(string userId, string title, string body);
    }

}
