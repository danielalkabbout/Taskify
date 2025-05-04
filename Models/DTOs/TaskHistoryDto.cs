namespace TaskManagementSystem.Models.DTOs
{
    public class TaskHistoryDto
    {
        public int Id { get; set; }
        public string ChangeDescription { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } 
        public string ChangedByUserId { get; set; } 
    }
    public class UserTaskHistoryDto
    {
        public int Id { get; set; }
        public string ChangeDescription { get; set; }
        public DateTime ChangedAt { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskStatus { get; set; }
        public string ChangeType { get; set; }
    }
}