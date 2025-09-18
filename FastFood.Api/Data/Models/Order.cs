using FastFood.Common.Enums;

namespace FoodFast.Data.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public int RestaurantId { get; set; }
        public string? DriverId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public int? EstimatedPreparationTime { get; set; }
        public virtual ApplicationUser Customer { get; set; }
        public virtual ApplicationUser Driver { get; set; }
        public virtual Restaurant Restaurant { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
