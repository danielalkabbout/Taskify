using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class AddCommentDto
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Text { get; set; }
    }

    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}