using FastFood.Api.DTOs;
using FastFood.Api.DTOs.chat;
using FastFood.Api.Services.Interfaces;
using FastFood.Common.Enums;
using FoodFast.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FastFood.Api.Hubs
{

    // ==========================================
    // FEATURE 5: CUSTOMER SUPPORT CHAT
    // Pattern: WEBSOCKETS (SignalR)
    // Reasoning: Real-time bi-directional messaging with typing indicators
    // ==========================================

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.SupportAgent)}"
    )]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // Called when user connects
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            _logger.LogInformation($"User {userId} connected to chat hub");

            // Join user to their personal room for notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // If support agent, join agents group
            if (userRole == UserRole.SupportAgent || userRole == UserRole.Admin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "support_agents");
            }

            await base.OnConnectedAsync();
        }

        // Called when user disconnects
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            _logger.LogInformation($"User {userId} disconnected from chat hub");

            // Remove from groups
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            if (userRole == UserRole.SupportAgent || userRole == UserRole.Admin)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "support_agents");
            }

            // Notify about user going offline in all their chat rooms
            await NotifyUserOffline(userId);

            await base.OnDisconnectedAsync(exception);
        }

        // Join a specific chat room
        public async Task JoinConversation(int ConversationId)
        {
            var userId = GetCurrentUserId();
            var Conversation = await _chatService.GetConversationAsync(ConversationId, userId);

            if (Conversation == null)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to chat room");
                return;
            }

            // Join the chat room group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{ConversationId}");

            // Mark messages as read
            await _chatService.MarkMessagesAsReadAsync(ConversationId, userId);

            // Notify others in the room that user joined
            await Clients.Group($"chat_{ConversationId}")
                .SendAsync("UserJoinedChat", new { ConversationId = ConversationId, UserId = userId });

            _logger.LogInformation($"User {userId} joined chat room {ConversationId}");
        }

        // Leave a specific chat room
        public async Task LeaveConversation(int ConversationId)
        {
            var userId = GetCurrentUserId();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{ConversationId}");

            // Notify others in the room that user left
            await Clients.Group($"chat_{ConversationId}")
                .SendAsync("UserLeftChat", new { ConversationId = ConversationId, UserId = userId });

            _logger.LogInformation($"User {userId} left chat room {ConversationId}");
        }

        // Send a message
        public async Task SendMessage(int ConversationId, string content, string messageType = "Text")
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrWhiteSpace(content))
                {
                    await Clients.Caller.SendAsync("Error", "Message cannot be empty");
                    return;
                }

                // Save message to database
                var message = await _chatService.SendMessageAsync(ConversationId, userId, content);

                // Send message to all users in the chat room
                await Clients.Group($"chat_{ConversationId}")
                    .SendAsync("ReceiveMessage", message);

                // Send notification to the other party if they're not in the room
                await NotifyMessageReceived(ConversationId, message, userId);

                _logger.LogInformation($"Message sent by user {userId} in chat room {ConversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        // Send typing indicator
        public async Task StartTyping(int ConversationId)
        {
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            var typingIndicator = new TypingIndicator
            {
                ConversationId = ConversationId,
                UserId = userId,
                IsTyping = true
            };

            // Notify others in the room (exclude sender)
            await Clients.GroupExcept($"chat_{ConversationId}", Context.ConnectionId)
                .SendAsync("TypingIndicator", typingIndicator);
        }

        public async Task StopTyping(int ConversationId)
        {
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            var typingIndicator = new TypingIndicator
            {
                ConversationId = ConversationId,
                UserId = userId,
                IsTyping = false
            };

            // Notify others in the room (exclude sender)
            await Clients.GroupExcept($"chat_{ConversationId}", Context.ConnectionId)
                .SendAsync("TypingIndicator", typingIndicator);
        }

        // Mark messages as read
        public async Task MarkAsRead(int ConversationId)
        {
            var userId = GetCurrentUserId();

            await _chatService.MarkMessagesAsReadAsync(ConversationId, userId);

            // Notify others that messages have been read
            await Clients.GroupExcept($"chat_{ConversationId}", Context.ConnectionId)
                .SendAsync("MessagesRead", new { ConversationId = ConversationId, ReadByUserId = userId });
        }

        // Agent assigns themselves to a chat
        public async Task AssignToChat(int ConversationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userRole != UserRole.SupportAgent && userRole != UserRole.Admin)
            {
                await Clients.Caller.SendAsync("Error", "Only support agents can assign chats");
                return;
            }

            var Conversation = await _chatService.AssignAgentToChatAsync(ConversationId, userId);
            if (Conversation != null)
            {
                // Notify customer that agent joined
                await Clients.Group($"user_{Conversation.CustomerId}")
                    .SendAsync("AgentAssigned", Conversation);

                // Notify all support agents that chat is now assigned
                await Clients.Group("support_agents")
                    .SendAsync("ChatAssigned", Conversation);

                _logger.LogInformation($"Agent {userId} assigned to chat room {ConversationId}");
            }
        }

        // Close a chat room
        public async Task CloseChat(int ConversationId)
        {
            var userId = GetCurrentUserId();

            await _chatService.CloseConversationAsync(ConversationId, userId);

            // Notify all users in the chat room
            await Clients.Group($"chat_{ConversationId}")
                .SendAsync("ChatClosed", new { ConversationId = ConversationId, ClosedByUserId = userId });

            _logger.LogInformation($"Chat room {ConversationId} closed by user {userId}");
        }

        // Private helper methods
        private string GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim ?? throw new Exception("User ID claim not found");
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Customer;
        }

        private string GetCurrentUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }

        private async Task NotifyMessageReceived(int ConversationId, ChatMessageDto message, string senderId)
        {
            // Get chat room details to find the other party
            var Conversation = await _chatService.GetConversationAsync(ConversationId, senderId);
            if (Conversation == null) return;

            // Determine who to notify
            string targetUserId = senderId == Conversation.CustomerId
                   ? Conversation.AgentId
                    : Conversation.CustomerId;


            if (string.IsNullOrEmpty(targetUserId))
            {
                // Send notification to the target user
                await Clients.Group($"user_{targetUserId}")
                    .SendAsync("NewMessageNotification", new
                    {
                        ConversationId = ConversationId,
                        Message = message,
                        Conversation = Conversation
                    });
            }
        }

        private async Task NotifyUserOffline(string userId)
        {
            // This would typically update database with last seen time
            // and notify relevant chat rooms
            await Clients.All.SendAsync("UserOffline", new { UserId = userId });
        }
    }
}

