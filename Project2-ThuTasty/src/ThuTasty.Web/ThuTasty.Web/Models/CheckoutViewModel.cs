using System.ComponentModel.DataAnnotations;

namespace ThuTasty.Web.Models
{
    public class CheckoutViewModel
    {
        [Required]
        public string CustomerName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
