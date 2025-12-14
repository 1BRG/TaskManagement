using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagement.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Rezumat AI
        public string? AISummary { get; set; }
        public DateTime? LastAISummaryDate { get; set; }

        // Foreign Key catre Organizator
        [Required]
        public string OrganizerId { get; set; }

        [ForeignKey("OrganizerId")]
        public virtual ApplicationUser Organizer { get; set; }

        // Relatii
        public virtual ICollection<ProjectMember> Members { get; set; }
        public virtual ICollection<ProjectColumn> Columns { get; set; }
        public virtual ICollection<AppTask> Tasks { get; set; }
    }
}