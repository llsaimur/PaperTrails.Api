namespace PaperTrails.Api.DTOs.ChatAssistant
{
    public class AskAiRequest
    {
        public string Query { get; set; } = string.Empty;      
        public string DocumentText { get; set; } = string.Empty; 
    }
}
