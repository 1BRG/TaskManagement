using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagement.Models
{
    public class TaskImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } // Relative path to wwwroot/uploads

        public string OriginalFileName { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int AppTaskId { get; set; }
        
        [ForeignKey("AppTaskId")]
        public virtual AppTask AppTask { get; set; }
    }
}
