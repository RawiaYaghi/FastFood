using FastFood.Common.Enums;

namespace FoodFast.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        public UserRole Role { get; set; } = UserRole.Customer;

    }
}
