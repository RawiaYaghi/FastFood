using FastFood.Api.DTOs;
using FastFood.Api.Services.Interfaces;
using FastFood.Common.Enums;
using FoodFast.Data.Models;
using FoodFast.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace FastFood.Api.Controllers
{
    // ==========================================
    // FEATURE 4: RESTAURANT ORDER NOTIFICATIONS
    // Pattern: SERVER-SENT EVENTS (SSE)
    // Reasoning: One-way updates, efficient for notification streaming
    // ==========================================
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IConnectionMultiplexer _redis;

        public OrderController(IOrderService orderService, IConnectionMultiplexer redis)
        {
            _orderService = orderService;
            _redis = redis;
        }


        [HttpGet("stream/{restaurantId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserRole.Restaurant))]
        public async Task StreamOrders(int restaurantId)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");


            //GetSubscriber() = opens a "radio" for subscribing or publishing to Redis channels.
            var subscriber = _redis.GetSubscriber();
            var channel = $"restaurant:orders:{restaurantId}";

            await subscriber.SubscribeAsync(channel, async (ch, orderData) =>
            {
                // Send order immediately to all connected staff
                var data = $"data: {orderData}\n\n";
                await Response.WriteAsync(data);
                await Response.Body.FlushAsync();
            });

            // Keep connection alive
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(30000); // Heartbeat every 30 seconds
                await Response.WriteAsync(":heartbeat\n\n");
                await Response.Body.FlushAsync();
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserRole.Customer))]
        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderDto order)
        {
            // Save order to database and Notify restaurant immediately
            await _orderService.CreateOrderAsync(order);
            return Ok();
        }
    }
}