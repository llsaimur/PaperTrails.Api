namespace PaperTrails.Api.Services
{
    public interface IAiAssistantService
    {
        Task<string> AskAsync(string userId, string documentText, string query);
    }
}
