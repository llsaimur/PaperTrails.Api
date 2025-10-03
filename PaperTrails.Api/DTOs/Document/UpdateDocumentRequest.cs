namespace PaperTrails.Api.DTOs.Document
{
    public class UpdateDocumentRequest
    {
        /// <summary>
        /// Optional new file to replace the existing document in Paperless.
        /// </summary>
        public IFormFile? Document { get; set; }

        /// <summary>
        /// Optional new title for the document. 
        /// Will only update if provided (non-null and non-empty).
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Optional new description for the document.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional new category ID. If provided, the category's DocumentTypeId 
        /// will be used when calling Paperless.
        /// </summary>
        public string? CategoryId { get; set; }
    }
}
