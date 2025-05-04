using System;

namespace TaskManagementSystem.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

      
        public int TaskId { get; set; }
        public string UserId { get; set; }

        
        public Task Task { get; set; }
        public ApplicationUser User { get; set; }
    }
}