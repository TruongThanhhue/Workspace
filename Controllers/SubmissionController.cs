using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class SubmissionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SubmissionController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int taskId, string? linkUrl, string? note, IFormFile? file)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            string? fileUrl = null;

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "submissions");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                fileUrl = $"/uploads/submissions/{fileName}";
            }

            var submission = new Submission
            {
                TaskId = taskId,
                UserId = CurrentUserId(),
                FileUrl = fileUrl,
                LinkUrl = linkUrl?.Trim(),
                Note = note?.Trim(),
                Status = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.Submissions.Add(submission);

            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            // Notification: báo manager có bài nộp mới
            if (task?.Project != null)
            {
                var memberName = HttpContext.Session.GetString("UserName") ?? "Thành viên";
                _context.Notifications.Add(new Notification
                {
                    UserId = task.Project.ManagerId ?? 0,
                    Content = $"📬 {memberName} đã nộp nhiệm vụ \"{task.Title}\" — cần duyệt",
                    Url = $"/Task/Details/{taskId}",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Nộp nhiệm vụ thành công!";
            return RedirectToAction("Index", "Task", new { projectId = task?.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluate(int id, string status, string? feedback)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var submission = await _context.Submissions
                .Include(s => s.Task).ThenInclude(t => t!.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null) return NotFound();

            submission.Status = status;

            if (submission.Task != null)
            {
                if (status == "Approved")
                    submission.Task.Status = "Done";
                else if (status == "Rejected")
                    submission.Task.Status = "Pending";
            }

            // Notification: báo member kết quả duyệt
            if (submission.UserId != CurrentUserId())
            {
                var emoji = status == "Approved" ? "✅" : "❌";
                var statusLabel = status == "Approved" ? "duyệt" : "từ chối";
                var content = $"{emoji} Manager đã {statusLabel} bài nộp: \"{submission.Task?.Title}\"";
                if (!string.IsNullOrEmpty(feedback))
                    content += $" — {feedback}";

                _context.Notifications.Add(new Notification
                {
                    UserId = submission.UserId,
                    Content = content,
                    Url = $"/Task/Details/{submission.TaskId}",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            var label = status == "Approved" ? "Duyệt" : "Từ chối";
            TempData["Success"] = $"{label} bài nộp thành công!";
            return RedirectToAction("Details", "Task", new { id = submission.TaskId });
        }
    }
}