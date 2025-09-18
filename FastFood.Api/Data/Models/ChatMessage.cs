using FastFood.Common.Enums;

namespace FoodFast.Data.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public MessageStatus Status { get; set; } = MessageStatus.Sent;


        // Navigation properties
        public virtual Conversation Conversation { get; set; }
        public virtual ApplicationUser Sender { get; set; }

    }
}
