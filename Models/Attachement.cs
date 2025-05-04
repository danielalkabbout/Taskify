using System;

namespace TaskManagementSystem.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;


        public int TaskId { get; set; }
        public string UserId { get; set; }

        public Task Task { get; set; }
        public ApplicationUser User { get; set; }
    }
}