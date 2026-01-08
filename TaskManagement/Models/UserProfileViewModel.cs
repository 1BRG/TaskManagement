namespace TaskManagement.Models
{
    public class UserProfileViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Role { get; set; } // Ex: "Admin" sau "User"

        
        public int ProjectsCount { get; set; }
        public int TasksCount { get; set; }
        public int CommentsCount { get; set; }


        public ICollection<Project> Projects { get; set; }
        public ICollection<AppTask> Tasks { get; set; }

        public bool IsOwnerOrAdmin { get; set; }
    }
}
