using FastFood.Common.Enums;

namespace FoodFast.DTOs
{
    public class UpdateStatusDto
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
    }
}
