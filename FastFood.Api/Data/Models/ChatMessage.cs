namespace FoodFast.Data.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string ChatId { get; set; }
        public string SenderId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
