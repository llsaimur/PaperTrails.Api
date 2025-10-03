namespace PaperTrails.Api.DTOs.Paperless
{
    public class UpdateDocumentResult
    {
        public string? TaskId { get; set; }
        public int? PaperlessDocumentId { get; set; }
        public string Status { get; set; }
    }
}
