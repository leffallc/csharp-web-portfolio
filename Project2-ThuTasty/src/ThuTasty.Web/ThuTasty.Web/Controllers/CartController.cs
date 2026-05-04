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

        [HttpPost]
        public IActionResult Increase(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                item.Quantity++;
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Decrease(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();

            if (!ModelState.IsValid || !cart.Any())
            {
                return View(model);
            }

            var order = new Order
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                TotalAmount = cart.Sum(item => item.Price * item.Quantity),
                Items = cart.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                }).ToList()
            };

            order.PaymentStatus = "Unpaid";
            order.OrderStatus = "Pending";
            order.PaymentProvider = "Stripe";

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Clear cart
            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction("StripeCheckout", "Payments", new { orderId = order.Id });
        }

        public IActionResult Success()
        {
            return View();
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
