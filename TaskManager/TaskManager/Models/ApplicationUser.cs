using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigational Properties (Relații)
        public virtual ICollection<Project> OwnedProjects { get; set; } // Proiecte create
        public virtual ICollection<ProjectMember> ProjectsJoined { get; set; } // Proiecte unde e membru
        public virtual ICollection<AppTask> AssignedTasks { get; set; } // Task-uri asignate
    }
}