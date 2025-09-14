using FastFood.Common.Enums;

namespace FoodFast.Data.Models
{
    public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer; // default role
        public virtual ICollection<Order> Orders { get; set; }
    }



}
