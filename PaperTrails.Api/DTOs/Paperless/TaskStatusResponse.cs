using System;
using System.Text.Json.Serialization;

namespace PaperTrails.Api.DTOs.Paperless
{
    public class TaskStatusResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("task_id")]
        public string TaskId { get; set; } = default!;

        [JsonPropertyName("task_name")]
        public string TaskName { get; set; } = default!;

        [JsonPropertyName("task_file_name")]
        public string TaskFileName { get; set; } = default!;

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("date_done")]
        public DateTime? DateDone { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;

        [JsonPropertyName("result")]
        public string Result { get; set; } = default!;

        [JsonPropertyName("acknowledged")]
        public bool Acknowledged { get; set; }

        [JsonPropertyName("related_document")]
        public string? RelatedDocument { get; set; }

        [JsonPropertyName("owner")]
        public int Owner { get; set; }
    }
}
