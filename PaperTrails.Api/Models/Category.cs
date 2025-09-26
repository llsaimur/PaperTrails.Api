using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperTrails.Api.Models
{
    public class Category
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public string Description { get; set; }
        public int DocumentTypeId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreateddAt { get; set; } = DateTime.UtcNow;
    }
}
