using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ThuTasty.Web.Data;
using ThuTasty.Web.Models;

namespace ThuTasty.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(item => item.ProductId == product.Id);

            if (existingItem == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            }
            else
            {
                existingItem.Quantity++;
            }

            SaveCart(cart);

            return RedirectToAction("Index", "Home");
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }
    }
}
