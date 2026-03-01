using APIPSI16.Data;
using APIPSI16.Models;
using APIPSI16.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIPSI16.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require JWT for all actions
    public class ChatController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _context;

        public ChatController(xcleratesystemslinks_SampleDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/Chat
        // Users see only their own chats; admins see all
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            IQueryable<Chat> query = _context.Chats.Include(c => c.ChatUsers);

            // Non-admins see only chats they're part of
            if (userRole != "0")
            {
                if (!currentUserId.HasValue) return Unauthorized();
                query = query.Where(c => c.ChatUsers.Any(cu => cu.UserId == currentUserId.Value));
            }

            var chats = await query.ToListAsync();
            return Ok(chats);
        }

        // GET: api/Chat/5
        // Users can only see chats they're part of; admins can see any
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChat(int id)
        {
            var chat = await _context.Chats
                .Include(c => c.ChatUsers)
                .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(c => c.ChatId == id);

            if (chat == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Enforce ownership: users can only see chats they're part of
            if (userRole != "0" && !chat.ChatUsers.Any(cu => cu.UserId == currentUserId))
                return Forbid();

            return Ok(chat);
        }

        // GET: api/Chat/5/messages
        // Get paginated messages for a chat
        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetChatMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Check if user is part of the chat
            var isParticipant = await _context.ChatUsers
                .AnyAsync(cu => cu.ChatId == id && cu.UserId == currentUserId);

            if (userRole != "0" && !isParticipant)
                return Forbid("You are not a participant in this chat");

            var messages = await _context.ChatMessages
                .Where(cm => cm.ChatId == id)
                .OrderByDescending(cm => cm.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(cm => cm.SenderUser)
                .Select(cm => new ChatMessageDTO
                {
                    MessageId = cm.MessageId,
                    ChatId = cm.ChatId,
                    SenderUserId = cm.SenderUserId,
                    SenderName = cm.SenderUser.Name,
                    MessageText = cm.MessageText,
                    CreatedAt = cm.CreatedAt,
                    DeliveredAt = cm.DeliveredAt,
                    ReadAt = cm.ReadAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        // POST: api/Chat/5/messages
        // Send a message to a chat
        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDTO dto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var userRole = GetCurrentUserRole();

            // Check if user is part of the chat
            var isParticipant = await _context.ChatUsers
                .AnyAsync(cu => cu.ChatId == id && cu.UserId == currentUserId);

            if (userRole != "0" && !isParticipant)
                return Forbid("You are not a participant in this chat");

            var message = new ChatMessage
            {
                ChatId = id,
                SenderUserId = currentUserId.Value,
                MessageText = dto.MessageText,
                CreatedAt = DateTime.UtcNow,
                DeliveredAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        // PUT: api/Chat/5/messages/10/read
        // Mark a message as read
        [HttpPut("{chatId}/messages/{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int chatId, int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null || message.ChatId != chatId)
                return NotFound();

            // Only allow marking messages as read if you're not the sender
            if (message.SenderUserId == currentUserId.Value)
                return BadRequest("Cannot mark your own message as read");

            if (message.ReadAt == null)
            {
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // GET: api/Chat/conversations
        // Get chat list with unread counts for current user.
        // For Direct chats, ChatName = the OTHER participant's name.
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            // Load raw data first
            var chatMemberships = await _context.ChatUsers
                .Where(cu => cu.UserId == currentUserId.Value)
                .Include(cu => cu.Chat)
                    .ThenInclude(c => c.ChatMessages)
                        .ThenInclude(cm => cm.SenderUser)
                .Include(cu => cu.Chat)
                    .ThenInclude(c => c.ChatUsers)
                        .ThenInclude(cu2 => cu2.User)
                .ToListAsync();

            var result = chatMemberships.Select(cu =>
            {
                // For Direct chats, name = other participant's name
                string chatName;
                if (cu.Chat.Type == "Direct")
                {
                    var other = cu.Chat.ChatUsers
                        .FirstOrDefault(x => x.UserId != currentUserId.Value);
                    chatName = other?.User?.Name ?? "Mensagem Direta";
                }
                else
                {
                    chatName = cu.Chat.Type ?? "Grupo";
                }

                var lastMsg = cu.Chat.ChatMessages
                    .OrderByDescending(cm => cm.CreatedAt)
                    .FirstOrDefault();

                return new ChatListDTO
                {
                    ChatId = cu.ChatId,
                    ChatName = chatName,
                    UnreadCount = cu.Chat.ChatMessages.Count(cm =>
                        cm.SenderUserId != currentUserId.Value && cm.ReadAt == null),
                    LastMessage = lastMsg == null ? null : new ChatMessageDTO
                    {
                        MessageId = lastMsg.MessageId,
                        ChatId = lastMsg.ChatId,
                        SenderUserId = lastMsg.SenderUserId,
                        SenderName = lastMsg.SenderUser?.Name,
                        MessageText = lastMsg.MessageText,
                        CreatedAt = lastMsg.CreatedAt
                    }
                };
            }).ToList();

            return Ok(result);
        }

        // POST: api/Chat
        // All authenticated users can create chats
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] Chat chat)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChat), new { id = chat.ChatId }, chat);
        }

        // POST: api/Chat/with-participants
        // Creates a chat and adds all participants atomically (creator + others).
        // For Direct chats (2 participants), returns existing chat if one already exists.
        [HttpPost("with-participants")]
        public async Task<IActionResult> CreateChatWithParticipants([FromBody] CreateChatWithParticipantsDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            if (dto.ParticipantIds == null || !dto.ParticipantIds.Any())
                return BadRequest("ParticipantIds is required.");

            // Always include creator
            if (!dto.ParticipantIds.Contains(currentUserId.Value))
                dto.ParticipantIds.Insert(0, currentUserId.Value);

            var isDirect = dto.ParticipantIds.Count == 2;

            // Dedup: for Direct chats, return existing one if present (single query)
            if (isDirect)
            {
                var uid1 = dto.ParticipantIds[0];
                var uid2 = dto.ParticipantIds[1];

                // Load all direct chats that uid1 is part of, including ChatUsers
                var candidates = await _context.ChatUsers
                    .Where(cu => cu.UserId == uid1)
                    .Select(cu => cu.ChatId)
                    .ToListAsync();

                var existing = await _context.Chats
                    .Where(c => c.Type == "Direct" && candidates.Contains(c.ChatId))
                    .Include(c => c.ChatUsers)
                    .FirstOrDefaultAsync(c =>
                        c.ChatUsers.Count == 2 &&
                        c.ChatUsers.Any(cu => cu.UserId == uid2));

                if (existing != null)
                    return Ok(existing); // 200, not 201 – client navigates to existing chat
            }

            var chat = new Chat
            {
                CreatedByUserId = currentUserId.Value,
                CreatedAt = DateTime.UtcNow,
                Type = dto.ChatName ?? (isDirect ? "Direct" : "Group")
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync(); // generates ChatId

            foreach (var pid in dto.ParticipantIds)
            {
                _context.ChatUsers.Add(new ChatUser
                {
                    ChatId = chat.ChatId,
                    UserId = pid,
                    JoinedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChat), new { id = chat.ChatId }, chat);
        }

        // DELETE: api/Chat/5
        // Only admins can delete chats
        [HttpDelete("{id}")]
        [Authorize(Roles = "0")] // Admin only
        public async Task<IActionResult> DeleteChat(int id)
        {
            var chat = await _context.Chats.FindAsync(id);
            if (chat == null) return NotFound();

            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}