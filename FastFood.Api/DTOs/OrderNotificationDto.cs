using FastFood.Common.Enums;

namespace FastFood.Api.DTOs
{
    public class OrderNotificationDto
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int RestaurantId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public int? EstimatedPreparationTime { get; set; }
        public List<OrderItemNotificationDto> OrderItems { get; set; } = new();
        public string NotificationType { get; set; } = "NEW_ORDER";
    }

    public class OrderItemNotificationDto
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public string? SpecialInstructions { get; set; }
    }

}
