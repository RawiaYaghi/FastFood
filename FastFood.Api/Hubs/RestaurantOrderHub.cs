using FoodFast.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;


namespace FoodFast.Hubs
{
    // ==========================================
    // FEATURE 4: RESTAURANT ORDER NOTIFICATIONS
    // Pattern: WEBSOCKETS (SignalR)
    // Reasoning: Instant bi-directional communication, critical for orders
    // ==========================================
    [Authorize(Roles = "Restaurant")]
    public class RestaurantOrderHub : Hub
    {
        private readonly FoodFastDbContext _context;
        private readonly ILogger<RestaurantOrderHub> _logger;

        public RestaurantOrderHub(FoodFastDbContext context, ILogger<RestaurantOrderHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var restaurantId = Context.User.FindFirst("RestaurantId")?.Value;
            if (!string.IsNullOrEmpty(restaurantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant-{restaurantId}");
                _logger.LogInformation($"Restaurant {restaurantId} connected");
            }
            await base.OnConnectedAsync();
        }

        public async Task AcknowledgeOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = "Confirmed";
                order.AcknowledgedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Notify customer
                await Clients.Group($"customer-{order.CustomerId}")
                    .SendAsync("OrderConfirmed", orderId);
            }
        }

        public async Task UpdatePreparationTime(int orderId, int minutes)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.EstimatedPreparationTime = minutes;
                await _context.SaveChangesAsync();

                await Clients.Group($"customer-{order.CustomerId}")
                    .SendAsync("PreparationTimeUpdated", orderId, minutes);
            }
        }
    }
}
