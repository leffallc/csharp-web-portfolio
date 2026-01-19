using Microsoft.EntityFrameworkCore;
using Carl.TaskScheduler.Web.Models;

namespace Carl.TaskScheduler.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<TodoTask> TodoTasks => Set<TodoTask>();
    }
}
