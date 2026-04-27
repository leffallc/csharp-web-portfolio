using Microsoft.AspNetCore.Mvc;
using ThuTasty.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ThuTasty.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }
    }
}