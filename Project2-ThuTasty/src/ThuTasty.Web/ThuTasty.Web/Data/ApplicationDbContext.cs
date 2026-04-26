using Microsoft.EntityFrameworkCore;
using ThuTasty.Web.Models;

namespace ThuTasty.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
    }
}
