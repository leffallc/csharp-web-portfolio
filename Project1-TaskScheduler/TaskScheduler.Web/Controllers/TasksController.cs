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

        // GET: /Tasks/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _db.TodoTasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: /Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TodoTask task)
        {
            if (id != task.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(task);
            }

            _db.Update(task);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Tasks/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _db.TodoTasks.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: /Tasks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _db.TodoTasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            _db.TodoTasks.Remove(task);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Tasks/ToggleComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var task = await _db.TodoTasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            task.IsComplete = !task.IsComplete;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}