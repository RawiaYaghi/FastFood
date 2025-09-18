using FastFood.Api.DTOs;
using FastFood.Common.Enums;
using FoodFast.Data;
using FoodFast.Data.Models;
using FoodFast.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FastFood.Api.Services.Interfaces;


namespace FastFood.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderNotificationService _notificationService;
        private readonly FoodFastDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public OrderService(IOrderNotificationService notificationService, FoodFastDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _notificationService = notificationService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Order> CreateOrderAsync(CreateOrderDto orderDto)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // Create order logic here
            var order = new Order
            {
                CustomerId = user?.FindFirstValue("uid"),
                RestaurantId = orderDto.RestaurantId,
                DeliveryAddress = orderDto.DeliveryAddress,
                Status = OrderStatus.Preparing,
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderDto.OrderItems.Select(i => new OrderItem
                {
                    MenuItemId = i.MenuItemId,
                    Quantity = i.Quantity,
                    SpecialInstructions = i.SpecialInstructions
                }).ToList()
            };

            // Save to database
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();



            // Notify restaurant immediately
            await _notificationService.NotifyRestaurantNewOrder(order);

            return order;
        }

    }
}
