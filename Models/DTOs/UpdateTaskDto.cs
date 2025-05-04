using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class UpdateTaskDto
    {
        [StringLength(100)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [RegularExpression("Pending|InProgress|Completed", ErrorMessage = "Invalid status")]
        public string? Status { get; set; }

        [RegularExpression("Low|Medium|High", ErrorMessage = "Invalid priority")]
        public string? Priority { get; set; }

        public int? CategoryId { get; set; }

        public DateTime? ReminderDate { get; set; }

        [RegularExpression("Daily|Weekly|Monthly", ErrorMessage = "Invalid recurrence")]
        public string? Recurrence { get; set; }

        public List<string>? AssignedUserIds { get; set; }
    }
}