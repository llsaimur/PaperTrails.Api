using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperTrails.Api.Models
{
    public class Document
    {
        [Key]
        public string Id { get; set; }

        public string Title { get; set; }
        public string ContentUrl { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public string CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }

        public int DocumentId { get; set; }
        public string TaskId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public bool IsImportant { get; set; } = false;
    }
}
