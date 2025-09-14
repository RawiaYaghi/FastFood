namespace FoodFast.DTOs
{
    public class PaymentMethodDto
    {
        public string CardNumber { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string CVV { get; set; }
        public bool IsDefault { get; set; }
    }
}
