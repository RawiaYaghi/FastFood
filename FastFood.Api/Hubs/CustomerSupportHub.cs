using FoodFast.Data;
using FoodFast.Data.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FoodFast.Hubs
{
    public class CustomerSupportHub : Hub
    {
        private readonly FoodFastDbContext _context;
        private static readonly Dictionary<string, string> _activeChats = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task StartChat(string issue)
        {
            var userId = Context.User.FindFirst("UserId").Value;
            var chatId = Guid.NewGuid().ToString();

            var chat = new SupportChat
            {
                ChatId = chatId,
                CustomerId = int.Parse(userId),
                Issue = issue,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportChats.Add(chat);
            await _context.SaveChangesAsync();

            _activeChats[Context.ConnectionId] = chatId;

            // Notify available agents
            await Clients.Group("support-agents").SendAsync("NewChatRequest", new
            {
                chatId,
                customerName = Context.User.Identity.Name,
                issue
            });

            await Clients.Caller.SendAsync("ChatStarted", chatId);
        }

        public async Task SendMessage(string chatId, string message)
        {
            var chatMessage = new ChatMessage
            {
                ChatId = chatId,
                SenderId = Context.User.FindFirst("UserId").Value,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Send to chat participants
            await Clients.Group($"chat-{chatId}").SendAsync("ReceiveMessage", new
            {
                messageId = chatMessage.Id,
                sender = Context.User.Identity.Name,
                message,
                timestamp = chatMessage.Timestamp
            });

            // Send delivery confirmation
            await Clients.Caller.SendAsync("MessageDelivered", chatMessage.Id);
        }

        public async Task SendTypingIndicator(string chatId, bool isTyping)
        {
            await Clients.OthersInGroup($"chat-{chatId}")
                .SendAsync("TypingIndicator", Context.User.Identity.Name, isTyping);
        }

        public async Task JoinChat(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");

            // Load chat history
            var messages = await _context.ChatMessages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.Message,
                    m.Timestamp
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("ChatHistory", messages);
        }
    }
}
