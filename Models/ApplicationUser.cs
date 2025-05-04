using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public ICollection<Task> CreatedTasks { get; set; }
        public ICollection<UserTask> UserTasks { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Attachment> Attachments { get; set; }
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Employee = "Employee";
    }
}