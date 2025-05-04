using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public string Status { get; set; } 

        [Required]
        public string Priority { get; set; } 

        [Required]
        public int CategoryId { get; set; }

        public DateTime? ReminderDate { get; set; }
        public string? Recurrence { get; set; }
        public List<string>? AssignedUserIds { get; set; }
    }

}