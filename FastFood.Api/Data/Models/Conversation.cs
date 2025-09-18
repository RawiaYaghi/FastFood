using FastFood.Common.Enums;

namespace FoodFast.Data.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public string AgentId { get; set; }
        public string Subject { get; set; }

        public ConversationStatus Status { get; set; } = ConversationStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }



        // Navigation properties
        public virtual ApplicationUser Customer { get; set; }
        public virtual ApplicationUser Agent { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
    }
}
