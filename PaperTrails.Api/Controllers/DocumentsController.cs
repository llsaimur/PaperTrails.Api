using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaperTrails.Api.Data;
using PaperTrails.Api.DTOs;
using PaperTrails.Api.Models;
using PaperTrails.Api.Services;
using Microsoft.EntityFrameworkCore;
using PaperTrails.Api.DTOs.Document;
using System.Security.Claims;
using PaperTrails.Api.DTOs.Paperless;
using PaperTrails.Api.Enums;
using DocumentResponse = PaperTrails.Api.DTOs.Document.DocumentResponse;

namespace PaperTrails.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly PaperlessService _paperlessService;
        private readonly AppDbContext _db;
        private readonly string _paperlessUrl;

        public DocumentsController(PaperlessService paperlessService, AppDbContext db, IConfiguration configuration)
        {
            _paperlessService = paperlessService;
            _db = db;
            _paperlessUrl = configuration["Paperless:Url"];
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromForm] CreateDocumentRequest request)
        {
            if (request.Document == null || request.Document.Length == 0)
            {
                return BadRequest(new { error = "File is required" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            Category category = _db.Categories.FirstOrDefault(category => category.UserId ==  userId && category.Id == request.CategoryId);

            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            try
            {
                // Call Paperless API
                await using var stream = request.Document.OpenReadStream();

                string result = await _paperlessService.CreateDocument(
                    stream,
                    request.Document.FileName,
                    request.Title,
                    category.DocumentTypeId,
                    request.Description
                );

                // Save to DB
                var document = new Document
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = request.Title ?? request.Document.FileName,
                    Description = request.Description,
                    UserId = userId,
                    CategoryId = request.CategoryId,
                    TaskId = result,
                    ContentUrl = "PROCESSING",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Documents.Add(document);
                await _db.SaveChangesAsync();

                return Accepted(new
                {
                    message = "Document upload started!",
                    task_id = result,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetDocumentStatus([FromQuery] string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                return BadRequest(new { error = "Task ID is required" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            Document document = await _db.Documents.FirstOrDefaultAsync(document => document.TaskId == taskId && document.UserId == userId);

            if (document == null)
            {
                return NotFound(new { error = $"Document not found with Task ID : {taskId}" });
            }

            try
            {
                DTOs.Paperless.TaskStatusResponse result = await _paperlessService.GetDocumentStatusAsync(taskId);

                if (!Enum.TryParse(result.Status, out StatusResult statusResult))
                {
                    return StatusCode(500, new
                    {
                        error = $"Unknown document status received from Paperless: '{result.Status}'"
                    });
                }

                if (statusResult == StatusResult.FAILED || statusResult == StatusResult.FAILURE)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        error = "Document processing failed at Paperless."
                    });
                }


                if (statusResult == StatusResult.PENDING)
                {
                    return base.Ok(new DTOs.Document.TaskStatusResponse
                    {
                        Message = "Document is still being processed.",
                    });
                }

                if (statusResult == StatusResult.STARTED)
                {
                    return base.Ok(new DTOs.Document.TaskStatusResponse
                    {
                        Message = "Document processing has started.",
                    });
                }

                document.Status = result.Status;
                document.DocumentId = int.Parse(result.RelatedDocument);
                document.ContentUrl = $"{_paperlessUrl}/documents/{int.Parse(result.RelatedDocument)}/download/";
                document.UpdatedAt = DateTime.UtcNow;

                _db.Documents.Update(document);
                await _db.SaveChangesAsync();

                PaperlessDocumentResponse paperlessData = await _paperlessService.GetDocumentAsync(document.DocumentId);

                var updatedDocument = new DocumentResponse
                {
                    Id = document.Id,
                    Title = document.Title,
                    CategoryId = document.CategoryId,
                    DocumentId = document.DocumentId,
                    Description = document.Description,
                    Status = document.Status,
                    ContentUrl = document.ContentUrl,
                    TaskId = document.TaskId,
                    PaperlessData = paperlessData
                };

                return base.Ok(new DTOs.Document.TaskStatusResponse
                {
                    Message = $"Document '{document.Title}' has been uploaded successfully!",
                    Document = updatedDocument,
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var document = await _db.Documents.FirstOrDefaultAsync(document => document.Id == id && document.UserId == userId);
            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            try
            {
                // Delete in Paperless if already processed
                if (document.DocumentId != -1)
                {
                    await _paperlessService.DeleteDocumentAsync(document.DocumentId);
                }

                // Delete local DB record
                _db.Documents.Remove(document);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var document = await _db.Documents.FirstOrDefaultAsync(document => document.Id == id.ToString() && document.UserId == userId);

            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }
            try
            {
                PaperlessDocumentResponse result = await _paperlessService.GetDocumentAsync(document.DocumentId);

                var PaperlessDocument = new DocumentResponse
                {
                    Id = document.Id,
                    Title = document.Title,
                    Description = document.Description,
                    CategoryId = document.CategoryId,
                    DocumentId = document.DocumentId,
                    ContentUrl = document.ContentUrl,
                    Status = document.Status,
                    TaskId = document.TaskId,
                    PaperlessData = result
                };

                return Ok(PaperlessDocument);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to fetch Paperless document: {ex.Message}" });
            }
            
           
        }


        [HttpGet]
        public async Task<IActionResult> GetDocuments(
    [FromQuery] int page = 1,
    [FromQuery] int limit = 10,
    [FromQuery] string? categoryId = null,
    [FromQuery] bool? importantOnly = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            IQueryable<Document> query = _db.Documents.Where(d => d.UserId == userId);

            if (!string.IsNullOrEmpty(categoryId))
                query = query.Where(d => d.CategoryId == categoryId);

            if (importantOnly.HasValue && importantOnly.Value)
                query = query.Where(d => d.IsImportant);

            var total = await query.CountAsync();

            List<Document> documents = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            List<int> paperlessIds = documents
                .Where(d => d.DocumentId != -1)
                .Select(d => d.DocumentId)
                .ToList();

            var paperlessDataMap = new Dictionary<int, PaperlessDocumentResponse>();

            try
            {
                if (paperlessIds.Any())
                {
                    IEnumerable<PaperlessDocumentResponse> paperlessDocs =
                        await _paperlessService.GetAllDocumentsAsync(paperlessIds);
                    paperlessDataMap = paperlessDocs.ToDictionary(d => d.Id);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to fetch Paperless documents: {ex.Message}" });
            }

            var data = documents.Select(document => new DocumentResponse
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                CategoryId = document.CategoryId,
                DocumentId = document.DocumentId,
                ContentUrl = document.ContentUrl,
                Status = document.Status,
                TaskId = document.TaskId,
                IsImportant = document.IsImportant, // Include importance flag in response
                PaperlessData = document.DocumentId != -1 && paperlessDataMap.ContainsKey(document.DocumentId)
                    ? paperlessDataMap[document.DocumentId]
                    : null
            }).ToList();

            return Ok(new
            {
                page,
                limit,
                total,
                totalPages = (int)Math.Ceiling(total / (double)limit),
                data
            });
        }




        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetDocumentPdf(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var document = await _db.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            if (string.IsNullOrWhiteSpace(document.ContentUrl) || document.ContentUrl == "PROCESSING")
            {
                return BadRequest(new { error = "Document file not available yet" });
            }

            try
            {
                var pdfStream = await _paperlessService.GetDocumentPdfAsync(document.ContentUrl);

                return File(pdfStream, "application/pdf", $"{document.Title ?? "document"}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(string id, [FromForm] UpdateDocumentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            Document currentDocument = await _db.Documents.FirstOrDefaultAsync(document => document.Id == id && document.UserId == userId);

            if (currentDocument == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            var exists = await _db.Documents.AnyAsync(document =>
                document.Id != currentDocument.Id &&
                document.UserId == userId &&
                document.CategoryId == (request.CategoryId ?? currentDocument.CategoryId) &&
                document.Title == (request.Title ?? currentDocument.Title) &&
                document.DocumentId == currentDocument.DocumentId);

            if (exists)
            {
                return Conflict(new { error = "A document with the same title already exists in this category for this Paperless document." });
            }

            try
            {
                UpdateDocumentResult result = new();

                if ((request.Document != null && request.Document.Length > 0) || !string.IsNullOrWhiteSpace(request.CategoryId))
                {
                    Category category = await _db.Categories.FirstOrDefaultAsync(category => category.Id == request.CategoryId);

                    if (category == null)
                    {
                        return BadRequest(new { error = "Invalid category, category not found in database." });
                    }

                    await using var stream = request.Document?.OpenReadStream();

                    result = await _paperlessService.UpdateDocumentAsync(
                        currentDocument.DocumentId,
                        stream,
                        request.Document?.FileName,
                        request.Title,                
                        category.DocumentTypeId
                    );

                    currentDocument.TaskId = result.TaskId ?? currentDocument.TaskId;
                    currentDocument.CategoryId = request.CategoryId ?? category.Id;
                    currentDocument.DocumentId = result.PaperlessDocumentId ?? currentDocument.DocumentId;
                    currentDocument.Status = result.Status;

                    if (request.Document != null)
                    {
                        currentDocument.ContentUrl = "PROCESSING";
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    currentDocument.Title = request.Title;
                }

                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    currentDocument.Description = request.Description;
                }

                currentDocument.UpdatedAt = DateTime.UtcNow;

                _db.Documents.Update(currentDocument);
                await _db.SaveChangesAsync();

                PaperlessDocumentResponse paperlessData = null;
                if (currentDocument.Status == "SUCCESS") 
                {
                    paperlessData = await _paperlessService.GetDocumentAsync(currentDocument.DocumentId);
                }
                
                var updatedDocument = new DocumentResponse()
                {
                    Id = currentDocument.Id,
                    Title = currentDocument.Title,
                    Description = currentDocument.Description,
                    CategoryId = currentDocument.CategoryId,
                    DocumentId = currentDocument.DocumentId,
                    ContentUrl = currentDocument.ContentUrl,
                    Status = currentDocument.Status,
                    TaskId = currentDocument.TaskId,
                    PaperlessData = paperlessData
                };

                return Ok(new
                {
                    message = "Document updated successfully",
                    updatedDocument,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPatch("{id}/important")]
        public async Task<IActionResult> MarkDocumentImportant(string id, [FromQuery] bool important)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
            if (document == null)
                return NotFound(new { error = "Document not found" });

            document.IsImportant = important;
            document.UpdatedAt = DateTime.UtcNow;

            _db.Documents.Update(document);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Document marked as {(important ? "important" : "not important")}" });
        }


    }

}
