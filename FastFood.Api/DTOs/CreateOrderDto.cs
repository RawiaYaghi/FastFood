namespace FastFood.Api.DTOs
{
    public class CreateOrderDto
    {
        public int RestaurantId { get; set; }
        public string DeliveryAddress { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}
