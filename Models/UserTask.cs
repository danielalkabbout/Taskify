namespace TaskManagementSystem.Models
{
    public class UserTask
    {
        public string UserId { get; set; }
        public int TaskId { get; set; }

        
        public ApplicationUser User { get; set; }
        public Task Task { get; set; }
    }
}