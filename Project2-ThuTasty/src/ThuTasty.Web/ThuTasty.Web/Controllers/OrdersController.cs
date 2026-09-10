using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuTasty.Web.Data;

namespace ThuTasty.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Admin is viewing the orders list.");

                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Successfully loaded {OrderCount} orders.", orders.Count);

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load the orders list.");
                return View("Error");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("Admin is viewing details for OrderId {OrderId}.", id);

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("OrderId {OrderId} was not found.", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully loaded details for OrderId {OrderId}.", id);

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load details for OrderId {OrderId}.", id);
                return View("Error");
            }
        }
    }
}
