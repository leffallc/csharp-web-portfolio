using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThuTasty.Web.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }
    }
}
