using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace TaskManagementSystem.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? ReminderDate { get; set; }
        public TaskRecurrence? Recurrence { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int CategoryId { get; set; }
        public string CreatedByUserId { get; set; }

        public Category Category { get; set; }
        public ApplicationUser CreatedByUser { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Attachment> Attachments { get; set; }
        public ICollection<TaskHistory> TaskHistories { get; set; }
        public ICollection<UserTask> UserTasks { get; set; }
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High
    }

    public enum TaskRecurrence
    {
        Daily,
        Weekly,
        Monthly
    }
}