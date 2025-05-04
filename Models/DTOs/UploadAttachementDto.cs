using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class UploadAttachmentDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}