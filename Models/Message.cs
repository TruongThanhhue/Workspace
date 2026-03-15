namespace test.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;
        public int? ReceiverId { get; set; }
        public User? Receiver { get; set; }
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}