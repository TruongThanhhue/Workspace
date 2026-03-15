using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;

namespace test.Controllers
{
    public class TimelineController : Controller
    {
        private readonly AppDbContext _context;
        public TimelineController(AppDbContext context) => _context = context;

        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        public async Task<IActionResult> Index(int? projectId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var projects = await _context.Projects
                .Where(p => p.ManagerId == CurrentUserId())
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Projects = projects;
            ViewBag.SelectedProjectId = projectId;
            ViewBag.IsManager = HttpContext.Session.GetString("UserRole") == "Manager";
            return View();
        }

        public async Task<IActionResult> GetTasks(int projectId)
        {
            if (!IsLoggedIn()) return Unauthorized();

            var tasks = await _context.Tasks
                .Include(t => t.AssignedUser)
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            var events = tasks.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                start = t.StartDate.ToString("yyyy-MM-dd"),
                end = t.EndDate.AddDays(1).ToString("yyyy-MM-dd"),
                color = t.Priority switch
                {
                    "High" => "#dc3545",
                    "Medium" => "#fd7e14",
                    _ => "#0d6efd"
                },
                extendedProps = new
                {
                    status = t.Status,
                    assignedUser = t.AssignedUser?.Name ?? "Chưa giao",
                    priority = t.Priority ?? "Low",
                    description = t.Description ?? "",
                    startDisplay = t.StartDate.ToString("dd/MM/yyyy"),
                    endDisplay = t.EndDate.ToString("dd/MM/yyyy"),
                    startRaw = t.StartDate.ToString("yyyy-MM-dd"),
                    endRaw = t.EndDate.ToString("yyyy-MM-dd")
                }
            });

            return Json(events);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTask([FromBody] UpdateTaskDto dto)
        {
            if (!IsLoggedIn()) return Unauthorized();

            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == dto.Id);

            if (task == null) return NotFound();
            if (task.Project?.ManagerId != CurrentUserId()) return Forbid();

            task.StartDate = dto.Start.Date;
            task.EndDate = dto.End.AddDays(-1).Date; // -1 vì FullCalendar end là exclusive
            await _context.SaveChangesAsync();

            return Ok(new
            {
                startDisplay = task.StartDate.ToString("dd/MM/yyyy"),
                endDisplay = task.EndDate.ToString("dd/MM/yyyy")
            });
        }
    }

    public class UpdateTaskDto
    {
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}