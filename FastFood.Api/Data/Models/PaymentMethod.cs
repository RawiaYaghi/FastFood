namespace FoodFast.Data.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string CardLastFour { get; set; }
        public string CardType { get; set; }
        public string EncryptedData { get; set; }
        public bool IsDefault { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
