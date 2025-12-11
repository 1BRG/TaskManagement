using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Models
{
    public class ProjectMember
    {
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.Now;
    }
}