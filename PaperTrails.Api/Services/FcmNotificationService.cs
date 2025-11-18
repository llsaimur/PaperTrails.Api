using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using PaperTrails.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace PaperTrails.Api.Services
{
    public class FcmNotificationService : INotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<FcmNotificationService> _logger;
        private static bool _initialized;

        public FcmNotificationService(AppDbContext dbContext, ILogger<FcmNotificationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            if (!_initialized)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("firebase-service-account.json")
                });
                _initialized = true;
            }
        }

        public async Task SendAsync(string userId, string title, string body)
        {
            var tokens = await _dbContext.UserDevices
                .Where(d => d.UserId == userId)
                .Select(d => d.DeviceToken)
                .ToListAsync();

            if (!tokens.Any())
            {
                _logger.LogWarning("No device tokens for user {UserId}", userId);
                return;
            }

            var message = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                }
            };

            var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
            _logger.LogInformation("Push sent to {Count} devices for user {UserId}. Success: {SuccessCount}, Failures: {FailureCount}",
                tokens.Count, userId, response.SuccessCount, response.FailureCount);
        }
    }
}
