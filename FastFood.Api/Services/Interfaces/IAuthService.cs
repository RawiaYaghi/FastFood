using FoodFast.Data.Models;

namespace FoodFast.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> GenerateJWT(ApplicationUser user);
        string EncryptPaymentData(string data);
    }
}
