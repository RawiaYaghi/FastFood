using FoodFast.Data.Models;
using FoodFast.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodFast.Services
{
    // ==========================================
    // FEATURE 5: CUSTOMER SUPPORT CHAT
    // Pattern: WEBSOCKETS (SignalR)
    // Reasoning: Real-time bi-directional messaging with typing indicators
    // ==========================================

    public class OrderNotificationService
    {
        private readonly IHubContext<RestaurantOrderHub> _hubContext;

        public async Task NotifyNewOrder(Order order)
        {
            // Send to all connected restaurant staff
            await _hubContext.Clients.Group($"restaurant-{order.RestaurantId}")
                .SendAsync("NewOrder", new
                {
                    orderId = order.Id,
                    customerName = order.CustomerName,
                    items = order.OrderItems.Select(i => new
                    {
                        name = i.ProductName,
                        quantity = i.Quantity,
                        notes = i.SpecialInstructions
                    }),
                    totalAmount = order.TotalAmount,
                    orderTime = order.CreatedAt,
                    deliveryAddress = order.DeliveryAddress
                });
        }
    }
}
