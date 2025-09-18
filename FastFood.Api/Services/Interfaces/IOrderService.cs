using FastFood.Api.DTOs;
using FastFood.Common.Enums;
using FoodFast.Data.Models;

namespace FastFood.Api.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(CreateOrderDto orderDto);
        // Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    }
}
