namespace TaskManagementSystem.Models.DTOs
{


    public class TaskDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string CategoryName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReminderDate { get; set; }
        public string? Recurrence { get; set; }
        public List<AssignedUserDto> AssignedUsers { get; set; }
        public List<CommentDto> Comments { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Author { get; set; }
        public string AuthorId { get; set; }
    }
    
    public class AssignedUserDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}