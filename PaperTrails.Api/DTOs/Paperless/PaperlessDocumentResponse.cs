using System.Text.Json.Serialization;

namespace PaperTrails.Api.DTOs.Paperless
{
    public class PaperlessDocumentResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("correspondent")]
        public string? Correspondent { get; set; }

        [JsonPropertyName("document_type")]
        public int DocumentType { get; set; }

        [JsonPropertyName("storage_path")]
        public string? StoragePath { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("content")]
        public string Content { get; set; } = default!;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("created")]
        public string Created { get; set; } = default!;

        [JsonPropertyName("created_date")]
        public string CreatedDate { get; set; } = default!;

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }

        [JsonPropertyName("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [JsonPropertyName("archive_serial_number")]
        public string? ArchiveSerialNumber { get; set; }

        [JsonPropertyName("original_file_name")]
        public string OriginalFileName { get; set; } = default!;

        [JsonPropertyName("archived_file_name")]
        public string ArchivedFileName { get; set; } = default!;

        [JsonPropertyName("owner")]
        public int Owner { get; set; }

        [JsonPropertyName("user_can_change")]
        public bool UserCanChange { get; set; }

        [JsonPropertyName("is_shared_by_requester")]
        public bool IsSharedByRequester { get; set; }

        [JsonPropertyName("notes")]
        public List<string> Notes { get; set; } = new();

        [JsonPropertyName("custom_fields")]
        public List<string> CustomFields { get; set; } = new();

        [JsonPropertyName("page_count")]
        public int? PageCount { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = default!;
    }

    public class AllPaperlessDocumentsResponse
    {
        public IEnumerable<PaperlessDocumentResponse> Results { get; set; } = Enumerable.Empty<PaperlessDocumentResponse>();
    }
}
