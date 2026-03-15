using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        public HomeController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var isManager = HttpContext.Session.GetString("UserRole") == "Manager";
            var today = DateTime.Today;

            // Lấy projectId mà user tham gia (là manager HOẶC là member)
            var memberProjectIds = await _context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync();

            var managerProjectIds = await _context.Projects
                .Where(p => p.ManagerId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var myProjectIds = memberProjectIds.Union(managerProjectIds).ToList();

            // Tasks thuộc các project đó VÀ được giao cho user này
            IQueryable<TaskItem> myTasksQuery = _context.Tasks
                .Where(t => myProjectIds.Contains(t.ProjectId) && t.AssignedTo == userId);

            var model = new HomeViewModel
            {
                TotalProjects = myProjectIds.Count,
                TotalTasks = await myTasksQuery.CountAsync(),
                CompletedTasks = await myTasksQuery.CountAsync(t => t.Status == "Done"),
                OverdueTasks = await myTasksQuery.CountAsync(t => t.EndDate < today && t.Status != "Done"),
                UpcomingTasks = await myTasksQuery.CountAsync(t => t.EndDate >= today && t.EndDate <= today.AddDays(3) && t.Status != "Done"),

                RecentProjects = await _context.Projects
                    .Include(p => p.Manager)
                    .Include(p => p.Tasks)
                    .Where(p => myProjectIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                RecentTasks = await _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.AssignedUser)
                    .Where(t => myProjectIds.Contains(t.ProjectId) && t.AssignedTo == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            ViewBag.IsManager = isManager;
            return View(model);
        }
    }
}