using FastFood.Common.Enums;
using FoodFast.Data.Models;

namespace FastFood.Api.Services.Interfaces
{
    public interface IOrderNotificationService
    {
        Task NotifyRestaurantNewOrder(Order order);

    }
}
