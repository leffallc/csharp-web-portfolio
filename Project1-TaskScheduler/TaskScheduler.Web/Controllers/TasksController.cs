using Microsoft.AspNetCore.Mvc;
using Carl.TaskScheduler.Web.Models;

namespace TaskScheduler.Web.Controllers
{
    public class TasksController : Controller
    {
        public IActionResult Index()
        {
            var tasks = new List<TodoTask>
            {
                new TodoTask { Id = 1, Title = "Install equipment", IsComplete = false },
                new TodoTask { Id = 2, Title = "Run cable", IsComplete = true },
                new TodoTask { Id = 3, Title = "Test signal", IsComplete = false }
            };
            return View(tasks);
        }
    }
}
