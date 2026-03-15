using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        // GET: /Chat/Group/5 (group chat của project)
        public async Task<IActionResult> Group(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var project = await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            // Kiểm tra quyền truy cập
            var canAccess = project.ManagerId == CurrentUserId() ||
                            project.Members.Any(m => m.UserId == CurrentUserId());
            if (!canAccess)
            {
                TempData["Error"] = "Bạn không có quyền truy cập chat này!";
                return RedirectToAction("Details", "Project", new { id });
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ProjectId == id && m.ReceiverId == null)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.Project = project;
            ViewBag.Messages = messages;
            ViewBag.CurrentUserId = CurrentUserId();
            ViewBag.ChatType = "group";
            ViewBag.ReceiverId = null;

            return View("Chat");
        }

        // GET: /Chat/Private/5?receiverId=2
        public async Task<IActionResult> Private(int id, int receiverId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var project = await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var canAccess = project.ManagerId == CurrentUserId() ||
                            project.Members.Any(m => m.UserId == CurrentUserId());
            if (!canAccess)
            {
                TempData["Error"] = "Bạn không có quyền truy cập chat này!";
                return RedirectToAction("Details", "Project", new { id });
            }

            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null) return NotFound();

            var currentId = CurrentUserId();
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ProjectId == id &&
                    ((m.SenderId == currentId && m.ReceiverId == receiverId) ||
                     (m.SenderId == receiverId && m.ReceiverId == currentId)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.Project = project;
            ViewBag.Messages = messages;
            ViewBag.CurrentUserId = currentId;
            ViewBag.ChatType = "private";
            ViewBag.ReceiverId = receiverId;
            ViewBag.ReceiverName = receiver.Name;

            return View("Chat");
        }

        // POST: /Chat/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int projectId, string content, int? receiverId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (string.IsNullOrWhiteSpace(content))
                return receiverId == null
                    ? RedirectToAction("Group", new { id = projectId })
                    : RedirectToAction("Private", new { id = projectId, receiverId });

            var message = new Message
            {
                ProjectId = projectId,
                SenderId = CurrentUserId(),
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return receiverId == null
                ? RedirectToAction("Group", new { id = projectId })
                : RedirectToAction("Private", new { id = projectId, receiverId });
        }
        // GET: /Chat/GetMessages?projectId=5&receiverId=null
        [HttpGet]
        public async Task<IActionResult> GetMessages(int projectId, int? receiverId)
        {
            if (!IsLoggedIn()) return Unauthorized();
            var currentId = CurrentUserId();

            List<Message> messages;
            if (receiverId == null)
            {
                messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ProjectId == projectId && m.ReceiverId == null)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
            }
            else
            {
                messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ProjectId == projectId &&
                        ((m.SenderId == currentId && m.ReceiverId == receiverId) ||
                         (m.SenderId == receiverId && m.ReceiverId == currentId)))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
            }

            var result = messages.Select(m => new {
                m.Id,
                m.Content,
                m.SentAt,
                SenderName = m.Sender.Name,
                IsMine = m.SenderId == currentId
            });

            return Json(result);
        }
    }
}