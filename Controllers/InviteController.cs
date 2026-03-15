using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class InviteController : Controller
    {
        private readonly AppDbContext _context;
        public InviteController(AppDbContext context) => _context = context;

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        // POST: /Invite/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var invite = await _context.ProjectInvites
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id && i.InvitedUserId == CurrentUserId());

            if (invite == null || invite.Status != "Pending")
                return RedirectToAction("Index", "Notification");

            invite.Status = "Accepted";

            // Thêm vào ProjectMembers nếu chưa có
            var alreadyMember = await _context.ProjectMembers
                .AnyAsync(m => m.ProjectId == invite.ProjectId && m.UserId == CurrentUserId());

            if (!alreadyMember)
            {
                _context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = invite.ProjectId,
                    UserId = CurrentUserId(),
                    Role = "Member",
                    JoinedAt = DateTime.Now
                });
            }

            // Thông báo cho manager
            _context.Notifications.Add(new Notification
            {
                UserId = invite.InvitedByUserId,
                Content = $"{HttpContext.Session.GetString("UserName")} đã chấp nhận lời mời vào dự án \"{invite.Project?.Name}\"",
                Url = $"/Project/Details/{invite.ProjectId}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Bạn đã tham gia dự án \"{invite.Project?.Name}\"!";
            return RedirectToAction("Index", "Notification");
        }

        // POST: /Invite/Decline/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var invite = await _context.ProjectInvites
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id && i.InvitedUserId == CurrentUserId());

            if (invite == null || invite.Status != "Pending")
                return RedirectToAction("Index", "Notification");

            invite.Status = "Declined";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã từ chối lời mời.";
            return RedirectToAction("Index", "Notification");
        }

        // GET: /Invite/SearchUser?email=xxx&projectId=yyy
        public async Task<IActionResult> SearchUser(string email, int projectId)
        {
            if (!IsLoggedIn()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(email))
                return Json(new List<object>());

            // Lấy danh sách user đã là member
            var memberIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .Select(m => m.UserId)
                .ToListAsync();

            // Lấy danh sách user đã được invite pending
            var invitedIds = await _context.ProjectInvites
                .Where(i => i.ProjectId == projectId && i.Status == "Pending")
                .Select(i => i.InvitedUserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.Email.Contains(email)
                    && u.Id != CurrentUserId()
                    && !memberIds.Contains(u.Id)
                    && !invitedIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.Email, u.Role })
                .Take(5)
                .ToListAsync();

            return Json(users);
        }

        // POST: /Invite/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int projectId, int userId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.ManagerId != CurrentUserId())
            {
                TempData["Error"] = "Không có quyền mời!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            // Kiểm tra đã invite chưa
            var existing = await _context.ProjectInvites
                .AnyAsync(i => i.ProjectId == projectId && i.InvitedUserId == userId && i.Status == "Pending");
            if (existing)
            {
                TempData["Error"] = "Người dùng này đã được mời rồi!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            // Kiểm tra đã là member chưa
            var isMember = await _context.ProjectMembers
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
            if (isMember)
            {
                TempData["Error"] = "Người dùng này đã là thành viên!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            var invite = new ProjectInvite
            {
                ProjectId = projectId,
                InvitedUserId = userId,
                InvitedByUserId = CurrentUserId(),
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _context.ProjectInvites.Add(invite);
            await _context.SaveChangesAsync();

            // Gửi thông báo cho member
            var invitedUser = await _context.Users.FindAsync(userId);
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Content = $"Bạn được mời tham gia dự án \"{project.Name}\" bởi {HttpContext.Session.GetString("UserName")}",
                Url = $"/Invite/Respond/{invite.Id}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gửi lời mời tới {invitedUser?.Name ?? invitedUser?.Email}!";
            return RedirectToAction("Details", "Project", new { id = projectId });
        }

        // GET: /Invite/Respond/5  — trang chấp nhận/từ chối
        public async Task<IActionResult> Respond(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var invite = await _context.ProjectInvites
                .Include(i => i.Project)
                .Include(i => i.InvitedByUser)
                .FirstOrDefaultAsync(i => i.Id == id && i.InvitedUserId == CurrentUserId());

            if (invite == null) return NotFound();
            return View(invite);
        }
    }
}