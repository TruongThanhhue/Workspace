using Microsoft.AspNetCore.SignalR;
using test.Data;
using test.Models;

namespace test.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task JoinProject(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
        }

        public async Task SendGroupMessage(int projectId, int senderId, string senderName, string content)
        {
            var message = new Message
            {
                ProjectId = projectId,
                SenderId = senderId,
                ReceiverId = null,
                Content = content,
                SentAt = DateTime.Now
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group($"project_{projectId}").SendAsync("ReceiveGroupMessage", new
            {
                id = message.Id,
                senderName,
                content,
                sentAt = message.SentAt.ToString("HH:mm dd/MM")
            });
        }

        public async Task SendPrivateMessage(int projectId, int senderId, string senderName, int receiverId, string receiverName, string content)
        {
            var message = new Message
            {
                ProjectId = projectId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.Now
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var roomId = $"private_{Math.Min(senderId, receiverId)}_{Math.Max(senderId, receiverId)}_{projectId}";
            await Clients.Group(roomId).SendAsync("ReceivePrivateMessage", new
            {
                id = message.Id,
                senderName,
                content,
                sentAt = message.SentAt.ToString("HH:mm dd/MM")
            });
        }

        public async Task JoinPrivateRoom(int userId1, int userId2, int projectId)
        {
            var roomId = $"private_{Math.Min(userId1, userId2)}_{Math.Max(userId1, userId2)}_{projectId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }
    }
}