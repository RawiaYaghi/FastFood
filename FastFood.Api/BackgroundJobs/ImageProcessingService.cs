using FoodFast.DTOs;
using FoodFast.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;

namespace FoodFast.BackgroundJobs
{
    public class ImageProcessingService : BackgroundService
    {
        private readonly IMessageQueueService _messageQueue;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ImageProcessingService> _logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _messageQueue.ConsumeAsync<ImageProcessingJob>(async job =>
            {
                var db = _redis.GetDatabase();

                try
                {
                    // Update status
                    await db.StringSetAsync($"job:{job.JobId}:status", "processing");
                    await db.StringSetAsync($"job:{job.JobId}:progress", "10");

                    // Step 1: Validate image
                    _logger.LogInformation($"Validating image {job.JobId}");
                    await Task.Delay(2000); // Simulate validation
                    await db.StringSetAsync($"job:{job.JobId}:progress", "30");

                    // Step 2: Resize image
                    _logger.LogInformation($"Resizing image {job.JobId}");
                    var resizedPaths = await ResizeImage(job.FilePath);
                    await db.StringSetAsync($"job:{job.JobId}:progress", "60");

                    // Step 3: Compress image
                    _logger.LogInformation($"Compressing image {job.JobId}");
                    // await CompressImages(resizedPaths);
                    await db.StringSetAsync($"job:{job.JobId}:progress", "80");

                    // Step 4: Upload to storage
                    _logger.LogInformation($"Storing image {job.JobId}");
                    // var urls = await StoreImages(resizedPaths, job.MenuItemId);
                    await db.StringSetAsync($"job:{job.JobId}:progress", "100");

                    // Complete
                    await db.StringSetAsync($"job:{job.JobId}:status", "completed");
                    //await db.StringSetAsync($"job:{job.JobId}:result",
                    //    System.Text.Json.JsonSerializer.Serialize(new
                    //    {
                    //        thumbnailUrl = urls["thumbnail"],
                    //        mediumUrl = urls["medium"],
                    //        largeUrl = urls["large"]
                    //    }));

                    // Clean up temp file
                    File.Delete(job.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing image {job.JobId}");
                    await db.StringSetAsync($"job:{job.JobId}:status", "failed");
                    await db.StringSetAsync($"job:{job.JobId}:error", ex.Message);
                }
            }, stoppingToken);
        }

        private async Task<Dictionary<string, string>> ResizeImage(string filePath)
        {
            // Using ImageSharp (free library) for image processing
            var result = new Dictionary<string, string>();

            using var image = await Image.LoadAsync(filePath);

            // Thumbnail: 150x150
            var thumb = image.Clone(x => x.Resize(150, 150));
            var thumbPath = filePath.Replace(".", "_thumb.");
            await thumb.SaveAsync(thumbPath);
            result["thumbnail"] = thumbPath;

            // Medium: 500x500
            var medium = image.Clone(x => x.Resize(500, 500));
            var mediumPath = filePath.Replace(".", "_medium.");
            await medium.SaveAsync(mediumPath);
            result["medium"] = mediumPath;

            // Large: 1000x1000
            var large = image.Clone(x => x.Resize(1000, 1000));
            var largePath = filePath.Replace(".", "_large.");
            await large.SaveAsync(largePath);
            result["large"] = largePath;

            return result;
        }
    }
}
