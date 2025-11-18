using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PaperTrails.Api.Services;
using PaperTrails.Api.DTOs.ChatAssistant;

namespace PaperTrails.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiAssistantController : ControllerBase
    {
        private readonly IAiAssistantService _aiService;

        public AiAssistantController(IAiAssistantService aiService)
        {
            _aiService = aiService;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException();
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AskAiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DocumentText))
                return BadRequest(new { error = "DocumentText is required" });
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "Query is required" });

            try
            {
                var userId = GetUserId();
                var result = await _aiService.AskAsync(userId, request.DocumentText, request.Query);
                return Ok(new { answer = result });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "AI service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
