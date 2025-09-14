using FoodFast.BackgroundJobs;
using FoodFast.DTOs;
using FoodFast.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace FoodFast.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuImageController : ControllerBase
    {
        private readonly ImageProcessingService _imageService;
        private readonly IMessageQueueService _messageQueue;
        private readonly IConnectionMultiplexer _redis;

        public MenuImageController(
            ImageProcessingService imageService,
            IMessageQueueService messageQueue,
            IConnectionMultiplexer redis)
        {
            _imageService = imageService;
            _messageQueue = messageQueue;
            _redis = redis;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Restaurant")]
        [RequestSizeLimit(10_485_760)] // 10MB limit
        public async Task<IActionResult> UploadMenuImage(IFormFile file, [FromForm] int menuItemId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Invalid file type");

            // Generate processing job ID
            var jobId = Guid.NewGuid().ToString();

            // Save original file temporarily
            var tempPath = Path.Combine(Path.GetTempPath(), jobId + Path.GetExtension(file.FileName));
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Initialize job status in Redis
            var db = _redis.GetDatabase();
            await db.StringSetAsync($"job:{jobId}:status", "queued", TimeSpan.FromHours(1));
            await db.StringSetAsync($"job:{jobId}:progress", "0", TimeSpan.FromHours(1));

            // Queue for background processing
            await _messageQueue.EnqueueAsync(new ImageProcessingJob
            {
                JobId = jobId,
                FilePath = tempPath,
                MenuItemId = menuItemId,
                RestaurantId = int.Parse(User.FindFirst("RestaurantId").Value)
            });

            // Return immediately with job ID
            return Accepted(new
            {
                jobId,
                message = "Image upload started",
                statusUrl = $"/api/menuimage/status/{jobId}"
            });
        }
    }
}
