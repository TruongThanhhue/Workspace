namespace test.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public TaskItem? Task { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}