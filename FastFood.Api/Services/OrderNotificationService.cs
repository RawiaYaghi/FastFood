using FastFood.Api.DTOs;
using FastFood.Api.Services.Interfaces;
using FoodFast.Data;
using FoodFast.Data.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Text.Json;
using Order = FoodFast.Data.Models.Order;

namespace FoodFast.Services
{


    public class OrderNotificationService : IOrderNotificationService
    {

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<OrderNotificationService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public OrderNotificationService(
            IConnectionMultiplexer redis,
            ILogger<OrderNotificationService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _redis = redis;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task NotifyRestaurantNewOrder(Order order)
        {
            try
            {
                var notification = await CreateOrderNotificationDto(order.Id);
                notification.NotificationType = "NEW_ORDER";

                var subscriber = _redis.GetSubscriber();
                var channel = $"restaurant:orders:{order.RestaurantId}";
                var message = JsonSerializer.Serialize(notification, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await subscriber.PublishAsync(channel, message);

                _logger.LogInformation($"New order notification sent to restaurant {order.RestaurantId} for order {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send new order notification for order {order.Id}");
                throw;
            }
        }

        private async Task<OrderNotificationDto> CreateOrderNotificationDto(int orderId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FoodFastDbContext>();

            // You might need to fetch additional data if not already loaded
            var notification = await context.Orders
           .Where(o => o.Id == orderId)
           .Select(o => new OrderNotificationDto
           {
               Id = o.Id,
               CustomerId = o.CustomerId,
               CustomerName = o.Customer != null
                   ? o.Customer.FirstName + " " + o.Customer.LastName
                   : "Unknown Customer",
               CustomerPhone = o.Customer != null ? o.Customer.PhoneNumber : "",
               RestaurantId = o.RestaurantId,
               Status = o.Status,
               TotalAmount = o.TotalAmount,
               DeliveryAddress = o.DeliveryAddress,
               CreatedAt = o.CreatedAt,
               EstimatedDelivery = o.EstimatedDelivery,
               EstimatedPreparationTime = o.EstimatedPreparationTime,
               OrderItems = o.OrderItems.Select(item => new OrderItemNotificationDto
               {
                   Id = item.Id,
                   MenuItemId = item.MenuItemId,
                   MenuItemName = item.MenuItem != null ? item.MenuItem.Name : "Unknown Item",
                   Quantity = item.Quantity,
                   SpecialInstructions = item.SpecialInstructions
               }).ToList()
           })
           .FirstOrDefaultAsync();

            return notification;
        }


    }
}
