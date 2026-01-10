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

        // Removing Priority
        // public PriorityEnum Priority { get; set; } = PriorityEnum.Medium;

        public int Order { get; set; }
        
        public bool IsArchived { get; set; }
        public DateTime? ArchivedDate { get; set; }
        
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        public string? AssignedToUserId { get; set; }
        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        // Foreign Key to BoardColumn
        public int? BoardColumnId { get; set; }
        [ForeignKey("BoardColumnId")]
        public virtual BoardColumn? BoardColumn { get; set; }

        public bool IsCompleted { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Media
        public string? MediaUrl { get; set; }
        public MediaTypeEnum? MediaType { get; set; }

        public ICollection<BoardLabel> Labels { get; set; } = new List<BoardLabel>();
        public ICollection<TaskImage> Images { get; set; } = new List<TaskImage>();
        public virtual ICollection<Comment> Comments { get; set; }
    }
}