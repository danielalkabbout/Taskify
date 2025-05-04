using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}