using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThuTasty.Web.Models;

namespace ThuTasty.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var hasher = new PasswordHasher<IdentityUser>();
            var admin_role = new IdentityRole{
                Id = "admin",
                Name = "Admin",
                NormalizedName = "ADMIN"
            };
            var admin_user = new IdentityUser {
                Id = "admin-user",
                UserName = "admin@example.com",
                NormalizedUserName = "ADMIN@EXAMPLE.COM",
                Email = "admin@example.com",
                NormalizedEmail = "ADMIN@EXAMPLE.COM",
                PasswordHash = hasher.HashPassword(null, "admin")
            };
            var user_role = new IdentityUserRole<string> {
                RoleId = admin_role.Id,
                UserId = admin_user.Id
            };
            modelBuilder.Entity<IdentityRole>().HasData(admin_role);
            modelBuilder.Entity<IdentityUser>().HasData(admin_user);
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(user_role);
        }
    }
}
