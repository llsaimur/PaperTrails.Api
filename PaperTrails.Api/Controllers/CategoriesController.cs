using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaperTrails.Api.Data;
using PaperTrails.Api.DTOs.Category;
using PaperTrails.Api.Models;
using PaperTrails.Api.Services;
using System.Security.Claims;

namespace PaperTrails.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PaperlessService _paperlessService;

        public CategoriesController(AppDbContext db, PaperlessService paperlessService)
        {
            _db = db;
            _paperlessService = paperlessService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var categoriesExist = await _db.Categories.AnyAsync(category => category.UserId == userId && category.Name == request.Name);

            if (categoriesExist)
            {
                return BadRequest(new { error = $"Creating a category failed: Category {request.Name} already exists" });
            }

            var paperlessResult = await _paperlessService.CreateCategory(request.Name);

            if (paperlessResult == null)
            {
                return BadRequest(new { error = "Creating a category failed" });
            }

            var category = new Category
            {
                Id = Guid.NewGuid().ToString(),
                Name = paperlessResult.Name,
                Description = request.Description,
                UserId = userId,
                DocumentTypeId = paperlessResult.Id
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DocumentTypeId = category.DocumentTypeId
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory([FromBody] CreateCategoryRequest request, string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id);
            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            var nameExists = await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == request.Name && c.Id != id);
            if (nameExists)
            {
                return BadRequest(new { error = $"Updating category failed: Category {request.Name} already exists" });
            }

            var paperlessResult = await _paperlessService.UpdateCategory(request.Name, category.DocumentTypeId);

            if (paperlessResult == null)
            {
                return BadRequest(new { error = "Updating category failed in Paperless service" });
            }

            category.Name = request.Name;
            category.Description = request.Description;
            category.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DocumentTypeId = category.DocumentTypeId
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var category = await _db.Categories
                .FirstOrDefaultAsync(category => category.Id == id && category.UserId == userId);

            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            return Ok(new
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DocumentTypeId = category.DocumentTypeId
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var skip = (page - 1) * pageSize;

            var totalCount = await _db.Categories.CountAsync(category => category.UserId == userId);

            var categories = await _db.Categories
                .Where(category => category.UserId == userId)
                .OrderBy(category => category.Name)
                .Skip(skip)
                .Take(pageSize)
                .Select(category => new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.DocumentTypeId,
                    category.CreateddAt,
                    category.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = categories
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id);
            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            var result = await _paperlessService.DeleteCategory(category.DocumentTypeId);
            if (!result.Success)
            {
                return BadRequest(new { error = "Failed to delete category in Paperless service" });
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Category '{category.Name}' deleted successfully." });
        }

    }


}
