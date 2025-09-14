namespace FoodFast.Data.Models
{
    public class SupportChat
    {
        public int Id { get; set; }
        public string ChatId { get; set; }
        public int CustomerId { get; set; }
        public int? AgentId { get; set; }
        public string Issue { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public virtual ApplicationUser Customer { get; set; }
        public virtual ApplicationUser Agent { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; }
    }
}
