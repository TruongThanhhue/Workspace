namespace test.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int? ManagerId { get; set; }
        public User? Manager { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public string? ImageUrl { get; set; }
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<JoinRequest> JoinRequests { get; set; } = new List<JoinRequest>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}