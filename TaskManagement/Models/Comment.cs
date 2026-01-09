using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagement.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Comentariul nu poate depasi 500 de caractere")]
        public string Content { get; set; }

        public DateTime PostedAt { get; set; } = DateTime.Now;

        // Relatii
        public int AppTaskId { get; set; }
        [ForeignKey("AppTaskId")]
        public virtual AppTask AppTask { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}