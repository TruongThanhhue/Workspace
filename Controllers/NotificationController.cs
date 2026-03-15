using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;

namespace test.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _db;
        public NotificationController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // Lấy tất cả thông báo
            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Đánh dấu tất cả là đã đọc
            var unread = notifications.Where(n => !n.IsRead).ToList();
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();

            return View(notifications);
        }
    }
}