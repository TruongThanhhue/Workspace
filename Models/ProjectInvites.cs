namespace test.Models
{
    public class ProjectInvite
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
        public int InvitedUserId { get; set; }
        public User? InvitedUser { get; set; }

        public int InvitedByUserId { get; set; }
        public User? InvitedByUser { get; set; }

        // Pending / Accepted / Declined
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}