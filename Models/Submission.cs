namespace test.Models
{
    public class Submission
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public TaskItem? Task { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string? FileUrl { get; set; }
        public string? LinkUrl { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}