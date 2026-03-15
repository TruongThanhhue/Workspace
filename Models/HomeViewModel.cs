using test.Models;

namespace test.Models
{
    public class HomeViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int UpcomingTasks { get; set; }
        public int CompletedTasks { get; set; }

        // Danh sách gần đây
        public List<Project> RecentProjects { get; set; } = new();
        public List<TaskItem> RecentTasks { get; set; } = new();
    }
}