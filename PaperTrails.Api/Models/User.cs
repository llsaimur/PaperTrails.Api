using System.ComponentModel.DataAnnotations;

namespace PaperTrails.Api.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }
        public string? PreviousEmail { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
