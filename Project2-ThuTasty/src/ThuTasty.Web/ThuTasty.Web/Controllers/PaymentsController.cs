using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using ThuTasty.Web.Data;

namespace ThuTasty.Web.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> StripeCheckout(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            var lineItems = order.Items.Select(item => new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = (long)(item.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName
                    }
                }
            }).ToList();

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = lineItems,
                SuccessUrl = $"{domain}/Payments/StripeSuccess?sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Cart"
            };

            var service = new SessionService();
            var session = service.Create(options);

            order.StripeSessionId = session.Id;
            order.PaymentProvider = "Stripe";
            order.PaymentStatus = "Pending";
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }

        public async Task<IActionResult> StripeSuccess(string sessionId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);

            if (order == null)
            {
                return NotFound();
            }

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Paid";
            order.PaymentProvider = "Stripe";

            await _context.SaveChangesAsync();

            return RedirectToAction("Success", "Cart");
        }
    }
}
