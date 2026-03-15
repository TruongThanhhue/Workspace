using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProjectController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private bool IsManager() => HttpContext.Session.GetString("UserRole") == "Manager";
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        // GET: /Project/Index
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var userId = CurrentUserId();

            var memberProjectIds = await _context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync();

            var projects = await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Tasks)
                .Where(p => p.ManagerId == userId || memberProjectIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // GET: /Project/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var project = await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Members).ThenInclude(m => m.User)
                .Include(p => p.JoinRequests).ThenInclude(j => j.User)
                .Include(p => p.Tasks).ThenInclude(t => t.AssignedUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            // Pending invites cho project này
            var pendingInvites = await _context.ProjectInvites
                .Include(i => i.InvitedUser)
                .Where(i => i.ProjectId == id && i.Status == "Pending")
                .ToListAsync();

            ViewBag.PendingInvites = pendingInvites;
            ViewBag.IsManager = IsManager() && project.ManagerId == CurrentUserId();
            return View(project);
        }

        // GET: /Project/Create
        public IActionResult Create()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsManager()) { TempData["Error"] = "Chỉ Manager mới có thể tạo dự án!"; return RedirectToAction("Index"); }
            return View(new ProjectViewModel());
        }

        // POST: /Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsManager()) return RedirectToAction("Index");
            if (!ModelState.IsValid) return View(model);
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
                return View(model);
            }

            var project = new Project
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                ManagerId = CurrentUserId(),
                CreatedAt = DateTime.Now
            };

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                var folder = Path.Combine(_env.WebRootPath, "img", "projects");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);
                project.ImageUrl = "/img/projects/" + fileName;
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Dự án \"{project.Name}\" đã được tạo thành công!";
            return RedirectToAction("Index");
        }

        // GET: /Project/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsManager()) { TempData["Error"] = "Chỉ Manager mới có thể chỉnh sửa!"; return RedirectToAction("Index"); }
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();
            if (project.ManagerId != CurrentUserId()) { TempData["Error"] = "Không có quyền!"; return RedirectToAction("Index"); }

            return View(new ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ExistingImageUrl = project.ImageUrl
            });
        }

        // POST: /Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsManager()) return RedirectToAction("Index");
            if (!ModelState.IsValid) return View(model);
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
                return View(model);
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();
            if (project.ManagerId != CurrentUserId()) { TempData["Error"] = "Không có quyền!"; return RedirectToAction("Index"); }

            project.Name = model.Name;
            project.Description = model.Description;
            project.StartDate = model.StartDate;
            project.EndDate = model.EndDate;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                var folder = Path.Combine(_env.WebRootPath, "img", "projects");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);
                project.ImageUrl = "/img/projects/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật dự án thành công!";
            return RedirectToAction("Index");
        }

        // POST: /Project/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsManager()) { TempData["Error"] = "Chỉ Manager mới có thể xóa!"; return RedirectToAction("Index"); }
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();
            if (project.ManagerId != CurrentUserId()) { TempData["Error"] = "Không có quyền!"; return RedirectToAction("Index"); }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa dự án \"{project.Name}\"!";
            return RedirectToAction("Index");
        }

        // POST: /Project/RequestJoin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestJoin(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var existing = await _context.JoinRequests.FirstOrDefaultAsync(j => j.ProjectId == id && j.UserId == CurrentUserId());
            if (existing != null) { TempData["Error"] = "Bạn đã gửi yêu cầu rồi!"; return RedirectToAction("Details", new { id }); }
            var isMember = await _context.ProjectMembers.AnyAsync(m => m.ProjectId == id && m.UserId == CurrentUserId());
            if (isMember) { TempData["Error"] = "Bạn đã là thành viên!"; return RedirectToAction("Details", new { id }); }

            _context.JoinRequests.Add(new JoinRequest { ProjectId = id, UserId = CurrentUserId(), Status = "Pending", CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi yêu cầu tham gia!";
            return RedirectToAction("Details", new { id });
        }

        // GET: /Project/JoinRequests/5
        public async Task<IActionResult> JoinRequests(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsManager()) { TempData["Error"] = "Chỉ Manager!"; return RedirectToAction("Index"); }
            var project = await _context.Projects.Include(p => p.JoinRequests).ThenInclude(j => j.User).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();
            if (project.ManagerId != CurrentUserId()) { TempData["Error"] = "Không có quyền!"; return RedirectToAction("Index"); }
            return View(project);
        }

        // POST: /Project/ApproveRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId, string action)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var request = await _context.JoinRequests.Include(j => j.Project).FirstOrDefaultAsync(j => j.Id == requestId);
            if (request == null) return NotFound();
            if (request.Project.ManagerId != CurrentUserId()) { TempData["Error"] = "Không có quyền!"; return RedirectToAction("Index"); }

            if (action == "approve")
            {
                request.Status = "Approved";
                _context.ProjectMembers.Add(new ProjectMember { ProjectId = request.ProjectId, UserId = request.UserId, JoinedAt = DateTime.Now });
                TempData["Success"] = "Đã duyệt yêu cầu!";
            }
            else
            {
                request.Status = "Rejected";
                TempData["Success"] = "Đã từ chối yêu cầu!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("JoinRequests", new { id = request.ProjectId });
        }

        // POST: /Project/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int projectId, int userId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.ManagerId != CurrentUserId()) { TempData["Error"] = "Không có quyền!"; return RedirectToAction("Index"); }

            var member = await _context.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
            if (member != null) { _context.ProjectMembers.Remove(member); await _context.SaveChangesAsync(); }

            TempData["Success"] = "Đã xóa thành viên!";
            return RedirectToAction("Details", new { id = projectId });
        }
    }
}