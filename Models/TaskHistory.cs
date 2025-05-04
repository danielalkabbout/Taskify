using System;

namespace TaskManagementSystem.Models
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public string ChangeDescription { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public int TaskId { get; set; }
        public string ChangedByUserId { get; set; }


        public Task Task { get; set; }
        public ApplicationUser ChangedByUser { get; set; }
    }
}