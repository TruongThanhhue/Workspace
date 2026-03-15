namespace test.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Done, Overdue
        public string? Priority { get; set; } // Low, Medium, High
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
        public int? AssignedTo { get; set; }
        public User? AssignedUser { get; set; }
        public List<Comment> Comments { get; set; } = new();
        public List<Submission> Submissions { get; set; } = new();
    }
}