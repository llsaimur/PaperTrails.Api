using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaperTrails.Api.Data;
using PaperTrails.Api.DTOs.UserAuthentication;
using PaperTrails.Api.Models;
using PaperTrails.Api.Services;
using System.Security.Claims;

namespace PaperTrails.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SupabaseService _supabaseService;

        public UsersController(AppDbContext db, SupabaseService supabaseService)
        {
            _db = db;
            _supabaseService = supabaseService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (_db.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest(new { error = "Email is already registered" });
            }

            var result = await _supabaseService.SignUpAsync(request.Email, request.Password);
            if (result == null || result.User == null)
                return BadRequest(new { error = "Registration failed" });

            var supabaseUser = result.User;
            var user = new User
            {
                Id = supabaseUser.Id,
                Name = request.Name,
                Email = supabaseUser.Email
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.CreatedAt
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _supabaseService.LoginAsync(request.Email, request.Password);
            if (result == null || result.AccessToken == null)
                return BadRequest(new { error = "Login failed" });

            var user = _db.Users.FirstOrDefault(_ => _.Email == result.User.Email);

            return Ok(new
            {
                Name = user.Name,
                Email = user.Email,
                Token = result.AccessToken,
                ExpiresIn = result.ExpiresIn
            });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMe()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = _db.Users.Find(userId);

            if (user == null)
            {
                var email = User.FindFirst("email")?.Value;
                var role = User.FindFirst("role")?.Value;

                return Ok(new { userId, email, role, message = "User not found in DB" });
            }

            return Ok(new { user.Id, user.Name, user.Email });
        }

        [HttpPost("password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordReset([FromBody] PasswordResetRequest request)
        {
            var success = await _supabaseService.SendPasswordResetEmailAsync(request.Email);
            if (!success)
            {
                return BadRequest(new { error = "Failed to send password reset email" });
            }

            return Ok(new { message = "Password reset email sent" });
        }

    }
}
