using Microsoft.AspNetCore.Mvc;

namespace PaperTrails.Api.DTOs.Document
{
    public class CreateDocumentRequest
    {
        [FromForm(Name = "document")]
        public IFormFile Document { get; set; }

        [FromForm(Name = "title")]
        public string? Title { get; set; }

        [FromForm(Name = "documentTypeId")]
        public int? DocumentTypeId { get; set; }

        [FromForm(Name = "description")]
        public string? Description { get; set; }

        [FromForm(Name = "categoryId")]
        public string? CategoryId { get; set; }
    }
}
