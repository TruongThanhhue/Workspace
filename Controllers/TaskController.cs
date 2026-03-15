using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class TaskController : Controller
    {
        private readonly AppDbContext _context;

        public TaskController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private bool IsManager() => HttpContext.Session.GetString("UserRole") == "Manager";
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        public async Task<IActionResult> Index(int? projectId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            if (projectId == null)
            {
                var myTasks = await _context.Tasks
                    .Include(t => t.Project)
                    .Where(t => t.AssignedTo == CurrentUserId())
                    .OrderBy(t => t.EndDate)
                    .ToListAsync();
                ViewBag.Project = null;
                ViewBag.IsManager = false;
                return View(myTasks);
            }

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            var canAccess = project.ManagerId == CurrentUserId() ||
                            project.Members.Any(m => m.UserId == CurrentUserId());
            if (!canAccess)
            {
                TempData["Error"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Project");
            }

            var tasks = await _context.Tasks
                .Include(t => t.AssignedUser)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Project = project;
            ViewBag.IsManager = project.ManagerId == CurrentUserId();
            return View(tasks);
        }

        public async Task<IActionResult> Create(int projectId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var project = await _context.Projects
                .Include(p => p.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (project.ManagerId != CurrentUserId())
            {
                TempData["Error"] = "Chỉ Manager mới có thể tạo nhiệm vụ!";
                return RedirectToAction("Index", new { projectId });
            }

            ViewBag.Project = project;
            ViewBag.Members = project.Members.Select(m => m.User).ToList();
            return View(new TaskViewModel { ProjectId = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var project = await _context.Projects
                .Include(p => p.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == model.ProjectId);

            if (project == null) return NotFound();
            if (project.ManagerId != CurrentUserId())
                return RedirectToAction("Index", new { projectId = model.ProjectId });

            if (!ModelState.IsValid)
            {
                ViewBag.Project = project;
                ViewBag.Members = project.Members.Select(m => m.User).ToList();
                return View(model);
            }

            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
                ViewBag.Project = project;
                ViewBag.Members = project.Members.Select(m => m.User).ToList();
                return View(model);
            }

            var task = new TaskItem
            {
                Title = model.Title,
                Description = model.Description,
                Status = model.Status,
                Priority = model.Priority,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                ProjectId = model.ProjectId,
                AssignedTo = model.AssignedTo,
                CreatedAt = DateTime.Now
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Notification: thông báo cho member được giao task (dùng task.Id sau SaveChanges)
            if (model.AssignedTo.HasValue && model.AssignedTo != CurrentUserId())
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = model.AssignedTo.Value,
                    Content = $"📋 Bạn được giao nhiệm vụ mới: \"{task.Title}\" trong dự án \"{project.Name}\"",
                    Url = $"/Task/Details/{task.Id}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            // Notification cho chính manager
            var assignedUser = project.Members.FirstOrDefault(m => m.UserId == model.AssignedTo)?.User;
            var assignedName = assignedUser?.Name ?? "thành viên";
            _context.Notifications.Add(new Notification
            {
                UserId = CurrentUserId(),
                Content = $"✅ Bạn đã giao nhiệm vụ \"{task.Title}\" cho {assignedName}",
                Url = $"/Task/Details/{task.Id}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo nhiệm vụ \"{task.Title}\"!";
            return RedirectToAction("Index", new { projectId = model.ProjectId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var task = await _context.Tasks
                .Include(t => t.Project).ThenInclude(p => p!.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();
            if (task.Project!.ManagerId != CurrentUserId())
            {
                TempData["Error"] = "Chỉ Manager mới có thể chỉnh sửa nhiệm vụ!";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            ViewBag.Project = task.Project;
            ViewBag.Members = task.Project.Members.Select(m => m.User).ToList();

            var model = new TaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority ?? "Medium",
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                ProjectId = task.ProjectId,
                AssignedTo = task.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var task = await _context.Tasks
                .Include(t => t.Project).ThenInclude(p => p!.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();
            if (task.Project!.ManagerId != CurrentUserId())
                return RedirectToAction("Index", new { projectId = task.ProjectId });

            if (!ModelState.IsValid)
            {
                ViewBag.Project = task.Project;
                ViewBag.Members = task.Project.Members.Select(m => m.User).ToList();
                return View(model);
            }

            var oldAssignedTo = task.AssignedTo;
            task.Title = model.Title;
            task.Description = model.Description;
            task.Status = model.Status;
            task.Priority = model.Priority;
            task.StartDate = model.StartDate;
            task.EndDate = model.EndDate;
            task.AssignedTo = model.AssignedTo;

            await _context.SaveChangesAsync();

            // Notification: nếu đổi người được giao
            if (model.AssignedTo.HasValue && model.AssignedTo != oldAssignedTo && model.AssignedTo != CurrentUserId())
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = model.AssignedTo.Value,
                    Content = $"📋 Bạn được giao nhiệm vụ: \"{task.Title}\" trong dự án \"{task.Project.Name}\"",
                    Url = $"/Task/Details/{task.Id}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            var assignedUser = task.Project.Members.FirstOrDefault(m => m.UserId == model.AssignedTo)?.User;
            var assignedName = assignedUser?.Name ?? "thành viên";
            _context.Notifications.Add(new Notification
            {
                UserId = CurrentUserId(),
                Content = $"✅ Bạn đã giao nhiệm vụ \"{task.Title}\" cho {assignedName}",
                Url = $"/Task/Details/{task.Id}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật nhiệm vụ thành công!";
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();
            if (task.Project!.ManagerId != CurrentUserId())
            {
                TempData["Error"] = "Chỉ Manager mới có thể xóa nhiệm vụ!";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            var projectId = task.ProjectId;
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa nhiệm vụ \"{task.Title}\"!";
            return RedirectToAction("Index", new { projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            task.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }

        public async Task<IActionResult> ManageTask()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var projects = await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.ManagerId == CurrentUserId())
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var task = await _context.Tasks
                .Include(t => t.Project).ThenInclude(p => p!.Members)
                .Include(t => t.AssignedUser)
                .Include(t => t.Submissions).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            var canAccess = task.Project!.ManagerId == CurrentUserId() ||
                            task.Project.Members.Any(m => m.UserId == CurrentUserId());
            if (!canAccess)
            {
                TempData["Error"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Project");
            }

            ViewBag.IsManager = task.Project.ManagerId == CurrentUserId();
            ViewBag.CurrentUserId = CurrentUserId();
            return View(task);
        }
    }
}