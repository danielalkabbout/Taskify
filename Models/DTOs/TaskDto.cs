using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string CategoryName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ReminderDate { get; set; }
        public string? Recurrence { get; set; }
    }

}