using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
using TaskManagement.Models;

namespace TaskManagement.Models
{
    public class AppTask
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul task-ului este obligatoriu")]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.NotStarted;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Media
        public string? MediaUrl { get; set; }
        public MediaTypeEnum? MediaType { get; set; }

        // Relatie cu Proiectul
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        // Relatie cu Membrul asignat (poate fi null)
        public string? AssignedToUserId { get; set; }
        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
    }
}