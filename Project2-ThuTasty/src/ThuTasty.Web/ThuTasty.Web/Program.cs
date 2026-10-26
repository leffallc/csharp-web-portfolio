using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThuTasty.Web.Data;
using ThuTasty.Web.Administration;

namespace ThuTasty.Web
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Reserve the admin command before ASP.NET interprets configuration arguments.
            var isAdminCommand = args.Length > 0 && args[0] == "admin";
            if (isAdminCommand && !AdminCommand.TryParse(args, out _))
            {
                Console.Error.WriteLine(AdminCommand.Usage);
                return 2;
            }

            var builder = WebApplication.CreateBuilder(isAdminCommand ? Array.Empty<string>() : args);
            if (isAdminCommand)
            {
                // Identity/EF diagnostics must never expose command credentials or reset tokens.
                builder.Logging.ClearProviders();
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSession();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            if (isAdminCommand)
            {
                builder.Services.AddScoped<AdministratorManager>();
                using var commandApp = builder.Build();
                using var scope = commandApp.Services.CreateScope();
                return await AdminCommand.RunAsync(args,
                    scope.ServiceProvider.GetRequiredService<AdministratorManager>());
            }

            // admin panel
            builder.Services.AddCoreAdmin("Admin");
            builder.Services.AddScoped<LegacyAdministratorStartupGuard>();

            await using var app = builder.Build();

            // Check before Run opens the HTTP listener. The operator command above
            // deliberately bypasses this guard so it can repair a fresh database.
            try
            {
                using var scope = app.Services.CreateScope();
                if (!await scope.ServiceProvider.GetRequiredService<LegacyAdministratorStartupGuard>().IsSafeAsync())
                {
                    Console.Error.WriteLine("Startup refused: the historical seeded credential is still present. Before starting the website, run: dotnet run --no-launch-profile -- admin rotate-existing-admin-password admin-user");
                    return 1;
                }
            }
            catch (Exception)
            {
                // Fail closed if the check cannot run; never print credential data.
                Console.Error.WriteLine("Startup refused: credential safety could not be checked. Check database availability and schema with your maintenance tools. No automatic migration was attempted.");
                return 1;
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            app.Run();
            return 0;
        }
    }
}
