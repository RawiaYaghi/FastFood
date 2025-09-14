using FoodFast.Data;
using FoodFast.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace FoodFast.Controllers
{
    // ==========================================
    // FEATURE 2: ORDER TRACKING FOR CUSTOMERS
    // Pattern: LONG POLLING
    // Reasoning: Battery efficient, updates feel real-time
    // ==========================================
    [ApiController]
    [Route("api/[controller]")]
    public class OrderTrackingController : ControllerBase
    {
        private readonly FoodFastDbContext _context;
        private readonly IConnectionMultiplexer _redis;

        public OrderTrackingController(FoodFastDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis;
        }

        [HttpGet("status/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderStatus(int orderId, [FromQuery] string lastStatus = null)
        {
            var timeout = TimeSpan.FromSeconds(30); // Long poll timeout
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return NotFound();

                // Check if status changed
                if (order.Status != lastStatus)
                {
                    return Ok(new
                    {
                        orderId = order.Id,
                        status = order.Status,
                        estimatedDelivery = order.EstimatedDelivery,
                        driverName = order.DriverName,
                        timestamp = DateTime.UtcNow
                    });
                }

                // Wait before checking again (battery efficient)
                await Task.Delay(2000); // Check every 2 seconds
            }

            // Return current status after timeout
            var currentOrder = await _context.Orders.FindAsync(orderId);
            return Ok(new
            {
                orderId = currentOrder.Id,
                status = currentOrder.Status,
                estimatedDelivery = currentOrder.EstimatedDelivery,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpPost("update-status")]
        [Authorize(Roles = "Restaurant,Driver")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
                return NotFound();

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            // Store in Redis for real-time updates
            var db = _redis.GetDatabase();
            await db.StringSetAsync($"order:{dto.OrderId}:status", dto.Status, TimeSpan.FromMinutes(60));

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated" });
        }
    }

}
