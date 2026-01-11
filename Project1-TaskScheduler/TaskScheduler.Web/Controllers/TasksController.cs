using Microsoft.AspNetCore.Mvc;
using Carl.TaskScheduler.Web.Models;

namespace Carl.TaskScheduler.Web.Controllers
{
    public class TasksController : Controller
    {
        private static readonly List<TodoTask> _tasks = new()
        {
            new TodoTask { Id = 1, Title = "Install equipment", IsComplete = false },
            new TodoTask { Id = 2, Title = "Run cable", IsComplete = true },
            new TodoTask { Id = 3, Title = "Test signal", IsComplete = false }
        };
        
        public IActionResult Index()
        {
            return View(_tasks);
        }

        [HttpGet]
        public IActionResult Create()
        { 
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TodoTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                ModelState.AddModelError(nameof(task.Title), "Title is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(task);
            }

            task.Id = _tasks.Count == 0 ? 1 : _tasks.Max(t => t.Id) + 1;
            _tasks.Add(task);

            return RedirectToAction(nameof(Index));
        }
    }
}
