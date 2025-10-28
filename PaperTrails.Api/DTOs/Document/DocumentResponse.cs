using PaperTrails.Api.DTOs.Paperless;

namespace PaperTrails.Api.DTOs.Document
{
    public class DocumentResponse
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string? CategoryId { get; set; }
        public int? DocumentId { get; set; }
        public string ContentUrl { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string TaskId { get; set; } = default!;
        public PaperlessDocumentResponse? PaperlessData { get; set; }
        public bool IsImportant { get; set; }
    }
}
