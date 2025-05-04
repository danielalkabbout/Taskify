using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Mail;

namespace TaskManagementSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Task> Tasks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserTask> UserTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship between User and Task
            modelBuilder.Entity<UserTask>()
                .HasKey(ut => new { ut.UserId, ut.TaskId });

            modelBuilder.Entity<UserTask>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTasks)
                .HasForeignKey(ut => ut.UserId);

            modelBuilder.Entity<UserTask>()
                .HasOne(ut => ut.Task)
                .WithMany(t => t.UserTasks)
                .HasForeignKey(ut => ut.TaskId);

            // Seed initial data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Work" },
                new Category { Id = 2, Name = "Personal" },
                new Category { Id = 3, Name = "Urgent" }
            );
        }
    }
}