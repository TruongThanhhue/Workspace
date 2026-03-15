using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class CommentController : Controller
    {
        private readonly AppDbContext _context;

        public CommentController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        // POST: /Comment/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int taskId, string content)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Details", "Task", new { id = taskId });

            var comment = new Comment
            {
                TaskId = taskId,
                UserId = CurrentUserId(),
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Task", new { id = taskId });
        }

        // POST: /Comment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            // Chỉ người viết hoặc manager mới xóa được
            var currentRole = HttpContext.Session.GetString("UserRole");
            if (comment.UserId != CurrentUserId() && currentRole != "Manager")
            {
                TempData["Error"] = "Bạn không có quyền xóa comment này!";
                return RedirectToAction("Details", "Task", new { id = comment.TaskId });
            }

            var taskId = comment.TaskId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Task", new { id = taskId });
        }
    }
}