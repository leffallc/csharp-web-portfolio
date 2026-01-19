using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Carl.TaskScheduler.Web.Data;
using Carl.TaskScheduler.Web.Models;

namespace Carl.TaskScheduler.Web.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TasksController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Tasks
        public async Task<IActionResult> Index()
        {
            var tasks = await _db.TodoTasks
                .OrderBy(t => t.IsComplete)
                .ThenBy(t => t.Id)
                .ToListAsync();

            return View(tasks);
        }

        // GET: /Tasks/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoTask task)
        {
            if (!ModelState.IsValid)
            {
                return View(task);
            }

            _db.TodoTasks.Add(task);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
    }
}