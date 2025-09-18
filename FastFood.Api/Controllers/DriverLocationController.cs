using FastFood.Common.Enums;
using FoodFast.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace FoodFast.Controllers
{

    // ==========================================
    // FEATURE 3: DRIVER LOCATION UPDATES
    // Pattern: SERVER-SENT EVENTS (SSE)
    // Reasoning: One-way updates, efficient for location streaming
    // ==========================================
    [ApiController]
    [Route("api/[controller]")]
    public class DriverLocationController : ControllerBase
    {
        //private readonly IDriverLocationService _locationService;
        private readonly IConnectionMultiplexer _redis;

        public DriverLocationController(IConnectionMultiplexer redis)
        {
            // _locationService = locationService;
            _redis = redis;
        }

        [HttpGet("stream/{orderId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = nameof(UserRole.Customer))]

        public async Task StreamDriverLocation(int orderId)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            var subscriber = _redis.GetSubscriber();
            var channel = $"driver:location:{orderId}";

            // Subscribe to Redis channel for location updates
            await subscriber.SubscribeAsync(channel, async (ch, location) =>
            {
                var data = $"data: {location}\n\n";
                await Response.WriteAsync(data);
                await Response.Body.FlushAsync();
            });

            // Keep connection alive
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(10000); // Heartbeat every 10 seconds
                await Response.WriteAsync(":heartbeat\n\n");
                await Response.Body.FlushAsync();
            }
        }

        [HttpPost("update")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = nameof(UserRole.Driver))]
        public async Task<IActionResult> UpdateDriverLocation([FromBody] LocationUpdateDto dto)
        {
            var db = _redis.GetDatabase();
            var subscriber = _redis.GetSubscriber();

            // Store latest location
            var locationJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                lat = dto.Latitude,
                lng = dto.Longitude,
                timestamp = DateTime.UtcNow,
                driverId = dto.DriverId
            });

            await db.StringSetAsync($"driver:{dto.DriverId}:location", locationJson, TimeSpan.FromMinutes(5));

            // Publish to subscribers
            await subscriber.PublishAsync($"driver:location:{dto.OrderId}", locationJson);

            return Ok();
        }
    }

}
