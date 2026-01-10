using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagement.Models
{
    public class BoardLabel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Color { get; set; } // Hex code or CSS class suffix

        // Link to Project so labels are project-scoped
        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        public virtual ICollection<AppTask> Tasks { get; set; }
    }
}
