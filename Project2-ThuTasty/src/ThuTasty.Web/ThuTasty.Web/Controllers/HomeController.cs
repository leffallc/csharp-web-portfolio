using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuTasty.Web.Data;
using ThuTasty.Web.Models;

namespace ThuTasty.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            var exceptionDetails =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionDetails?.Error != null)
            {
                _logger.LogError(
                    exceptionDetails.Error,
                    "Unhandled exception on path {Path}. TraceId: {TraceId}",
                    exceptionDetails.Path,
                    HttpContext.TraceIdentifier);
            }

            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}