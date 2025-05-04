using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class CreateNotificationDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; }
    }
}