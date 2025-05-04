namespace TaskManagementSystem.Models.DTOs
{
    public class UserDetailDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime DateCreated { get; set; }
        public List<string> Roles { get; set; }
    }
}