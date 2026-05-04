using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThuTasty.Web.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = "";

        [Required]
        public string Email { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<OrderItem> Items { get; set; } = new();

        public string OrderStatus { get; set; } = "Pending";

        public string PaymentStatus { get; set; } = "Unpaid";

        public string? StripeSessionId { get; set; }

        public string? PaymentProvider { get; set; }
    }
}
