using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<JoinRequest> JoinRequests { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProjectInvite> ProjectInvites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager).WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Project).WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser).WithMany()
                .HasForeignKey(t => t.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project).WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId);
            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.User).WithMany()
                .HasForeignKey(pm => pm.UserId);

            modelBuilder.Entity<JoinRequest>()
                .HasOne(j => j.Project).WithMany(p => p.JoinRequests)
                .HasForeignKey(j => j.ProjectId);
            modelBuilder.Entity<JoinRequest>()
                .HasOne(j => j.User).WithMany()
                .HasForeignKey(j => j.UserId);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender).WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver).WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Task).WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User).WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Task).WithMany(t => t.Submissions)
                .HasForeignKey(s => s.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Submission>()
                .HasOne(s => s.User).WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User).WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProjectInvite
            modelBuilder.Entity<ProjectInvite>()
                .HasOne(i => i.Project).WithMany()
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProjectInvite>()
                .HasOne(i => i.InvitedUser).WithMany()
                .HasForeignKey(i => i.InvitedUserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ProjectInvite>()
                .HasOne(i => i.InvitedByUser).WithMany()
                .HasForeignKey(i => i.InvitedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}