using System.Collections.Generic;

namespace TaskManagementSystem.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

   
        public ICollection<Task> Tasks { get; set; }
    }
}