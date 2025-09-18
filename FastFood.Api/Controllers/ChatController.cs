using FastFood.Api.DTOs.chat;
using FastFood.Api.Services.Interfaces;
using FastFood.Common.Enums;
using FoodFast.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FastFood.Api.Controllers
{

    // ==========================================
    // FEATURE 5: CUSTOMER SUPPORT CHAT
    // Pattern: WEBSOCKETS (SignalR)
    // Reasoning: Real-time bi-directional messaging with typing indicators
    // ==========================================
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new chat room (customer initiates support request)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                // Only customers can create chat rooms
                if (userRole != UserRole.Customer)
                {
                    return BadRequest("Only customers can initiate support chats");
                }

                var Conversation = await _chatService.CreateConversationAsync(userId, dto.InitialMessage);

                _logger.LogInformation($"Chat room created by customer {userId}");
                return Ok(Conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat room");
                return StatusCode(500, "Failed to create chat room");
            }
        }

        /// <summary>
        /// Get all chat rooms for current user
        /// </summary>
        [HttpGet("rooms")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var Conversations = await _chatService.GetUserConversationsAsync(userId, userRole);

                return Ok(Conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat rooms");
                return StatusCode(500, "Failed to get chat rooms");
            }
        }

        /// <summary>
        /// Get specific chat room details
        /// </summary>
        [HttpGet("rooms/{ConversationId}")]
        public async Task<IActionResult> GetConversation(int ConversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var Conversation = await _chatService.GetConversationAsync(ConversationId, userId);

                if (Conversation == null)
                {
                    return NotFound("Chat room not found or access denied");
                }

                return Ok(Conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat room {ConversationId}", ConversationId);
                return StatusCode(500, "Failed to get chat room");
            }
        }

        /// <summary>
        /// Get messages for a chat room with pagination
        /// </summary>
        [HttpGet("rooms/{ConversationId}/messages")]
        public async Task<IActionResult> GetChatMessages(int ConversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var messages = await _chatService.GetChatMessagesAsync(ConversationId, userId, page, pageSize);

                return Ok(new
                {
                    Messages = messages,
                    Page = page,
                    PageSize = pageSize,
                    HasMore = messages.Count == pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for chat room {ConversationId}", ConversationId);
                return StatusCode(500, "Failed to get messages");
            }
        }

        /// <summary>
        /// Send a message via REST API (alternative to SignalR)
        /// </summary>
        [HttpPost("rooms/{ConversationId}/messages")]
        public async Task<IActionResult> SendMessage(int ConversationId, [FromBody] SendMessageDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _chatService.SendMessageAsync(ConversationId, userId, dto.Content);

                _logger.LogInformation($"Message sent by user {userId} in chat room {ConversationId}");
                return Ok(message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chat room {ConversationId}", ConversationId);
                return StatusCode(500, "Failed to send message");
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        [HttpPost("rooms/{ConversationId}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int ConversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _chatService.MarkMessagesAsReadAsync(ConversationId, userId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for chat room {ConversationId}", ConversationId);
                return StatusCode(500, "Failed to mark messages as read");
            }
        }

        /// <summary>
        /// Get unassigned chats (for support agents)
        /// </summary>
        [HttpGet("unassigned")]
        [Authorize(Roles = "SupportAgent,Admin")]
        public async Task<IActionResult> GetUnassignedChats()
        {
            try
            {
                var unassignedChats = await _chatService.GetUnassignedChatsAsync();
                return Ok(unassignedChats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unassigned chats");
                return StatusCode(500, "Failed to get unassigned chats");
            }
        }

        /// <summary>
        /// Assign support agent to a chat
        /// </summary>
        [HttpPost("rooms/{ConversationId}/assign")]
        [Authorize(Roles = "SupportAgent,Admin")]
        public async Task<IActionResult> AssignAgentToChat(int ConversationId)
        {
            try
            {
                var agentId = GetCurrentUserId();
                var Conversation = await _chatService.AssignAgentToChatAsync(ConversationId, agentId);

                if (Conversation == null)
                {
                    return NotFound("Chat room not found");
                }

                _logger.LogInformation($"Agent {agentId} assigned to chat room {ConversationId}");
                return Ok(Conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning agent to chat room {ConversationId}", ConversationId);
                return StatusCode(500, "Failed to assign agent to chat");
            }
        }

        /// <summary>
        /// Close a chat room
        /// </summary>
        [HttpPost("rooms/{ConversationId}/close")]
        public async Task<IActionResult> CloseConversation(int ConversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _chatService.CloseConversationAsync(ConversationId, userId);

                if (!result)
                {
                    return NotFound("Chat room not found");
                }

                _logger.LogInformation($"Chat room {ConversationId} closed by user {userId}");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing chat room {ConversationId}", ConversationId);
                return StatusCode(500, "Failed to close chat room");
            }
        }

        // Helper methods
        private string GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim ?? throw new Exception("User ID claim not found");
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Customer;
        }
    }
}
