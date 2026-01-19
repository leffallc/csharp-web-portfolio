using System.ComponentModel.DataAnnotations;

namespace Carl.TaskScheduler.Web.Models
{
    public class TodoTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
        public bool IsComplete { get; set; }
    }
}
