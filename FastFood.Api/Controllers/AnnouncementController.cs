using FastFood.Common.Enums;
using FoodFast.Data;
using FoodFast.Data.Models;
using FoodFast.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace FoodFast.Controllers
{
    // ==========================================
    // FEATURE 6: SYSTEM-WIDE ANNOUNCEMENTS
    // Pattern: PUB/SUB with Redis
    // Reasoning: Scalable broadcasting, doesn't overwhelm server
    // ==========================================

    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly FoodFastDbContext _context;

        public AnnouncementController(IConnectionMultiplexer redis, FoodFastDbContext context)
        {
            _redis = redis;
            _context = context;
        }

        [HttpPost("broadcast")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> BroadcastAnnouncement([FromBody] AnnouncementDto dto)
        {
            var announcement = new Announcement
            {
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type, // "maintenance", "promotion", "feature"
                Priority = dto.Priority,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = dto.ExpiresAt
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // Publish to Redis channel
            var subscriber = _redis.GetSubscriber();
            var message = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = announcement.Id,
                title = announcement.Title,
                message = announcement.Message,
                type = announcement.Type,
                timestamp = announcement.CreatedAt
            });

            // Use different channels for different announcement types
            await subscriber.PublishAsync($"announcements:{dto.Type}", message);

            // Also store in cache for users who connect later
            var db = _redis.GetDatabase();
            await db.StringSetAsync(
                $"announcement:latest:{dto.Type}",
                message,
                dto.ExpiresAt - DateTime.UtcNow);

            return Ok(new { message = "Announcement broadcasted", announcementId = announcement.Id });
        }

        [HttpGet("subscribe")]
        [Authorize]
        public async Task SubscribeToAnnouncements()
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");

            var subscriber = _redis.GetSubscriber();

            // Subscribe to all announcement types
            await subscriber.SubscribeAsync("announcements:*", async (channel, message) =>
            {
                await Response.WriteAsync($"data: {message}\n\n");
                await Response.Body.FlushAsync();
            });

            // Send latest announcements on connection
            var db = _redis.GetDatabase();
            var types = new[] { "maintenance", "promotion", "feature" };

            foreach (var type in types)
            {
                var latest = await db.StringGetAsync($"announcement:latest:{type}");
                if (!latest.IsNullOrEmpty)
                {
                    await Response.WriteAsync($"data: {latest}\n\n");
                    await Response.Body.FlushAsync();
                }
            }

            // Keep alive
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(30000);
                await Response.WriteAsync(":ping\n\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}
