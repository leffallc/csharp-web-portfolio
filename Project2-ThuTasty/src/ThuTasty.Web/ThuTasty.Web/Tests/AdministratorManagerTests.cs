using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThuTasty.Web.Administration;
using ThuTasty.Web.Data;
using Xunit;

namespace ThuTasty.Web.Tests;

public sealed class AdministratorManagerTests
{
    private static string GenerateTestPassword(string? differentFrom = null)
    {
        string password;
        do
        {
            // 32 secure random bytes plus guaranteed lowercase, uppercase, digit,
            // and special characters exceed the configured Identity requirements.
            password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) + "aA1!";
        } while (password == differentFrom);

        return password;
    }

    [Fact]
    public async Task FreshMigrationsRestoreLegacyCredentialAndWebStartupRefusesIt()
    {
        await using var fixture = await Fixture.CreateAsync(migrate: true);
        Assert.False(await fixture.Guard.IsSafeAsync());

        // Exercise the real startup entry point against only this in-memory database.
        Assert.Equal(1, await Program.Main([
            "--ConnectionStrings:DefaultConnection", fixture.ConnectionString,
            "--environment", "Production"]));
        Assert.Single(await fixture.Database.Users.ToListAsync());
        Assert.Single(await fixture.Database.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task OperatorCanRotateFreshlyMigratedAdministratorBeforeWebStartup()
    {
        await using var fixture = await Fixture.CreateAsync(migrate: true);
        var original = await fixture.Users.FindByIdAsync("admin-user");
        Assert.NotNull(original);
        var originalHash = original.PasswordHash;
        var originalStamp = original.SecurityStamp;

        // A random, test-only password; no historical plaintext is needed.
        var password = GenerateTestPassword();
        Assert.True((await fixture.Manager.RotatePasswordAsync(original.Id, password)).Succeeded);
        fixture.Database.ChangeTracker.Clear();

        Assert.True(await fixture.Guard.IsSafeAsync());
        var rotated = await fixture.Users.FindByIdAsync(original.Id);
        Assert.NotNull(rotated);
        Assert.NotEqual(originalHash, rotated.PasswordHash);
        Assert.NotEqual(originalStamp, rotated.SecurityStamp);
        Assert.True(await fixture.Users.IsInRoleAsync(rotated, "Admin"));
        Assert.True(await fixture.Users.CheckPasswordAsync(rotated, password));
        Assert.Single(await fixture.Database.Users.ToListAsync());
        Assert.Single(await fixture.Database.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task MissingAdministratorIsAllowedAndCanBeExplicitlyProvisioned()
    {
        await using var fixture = await Fixture.CreateAsync(migrate: true);
        var legacy = await fixture.Users.FindByIdAsync("admin-user");
        Assert.NotNull(legacy);
        Assert.True((await fixture.Users.DeleteAsync(legacy)).Succeeded);

        Assert.True(await fixture.Guard.IsSafeAsync());
        Assert.Empty(await fixture.Database.Users.ToListAsync());
        Assert.Empty(await fixture.Database.UserRoles.ToListAsync());

        var password = GenerateTestPassword();
        Assert.True((await fixture.Manager.CreateAsync("operator@example.com", password)).Succeeded);
        Assert.True(await fixture.Guard.IsSafeAsync());
        var created = await fixture.Users.FindByEmailAsync("operator@example.com");
        Assert.NotNull(created);
        Assert.True(await fixture.Users.IsInRoleAsync(created, "Admin"));
        Assert.True(await fixture.Users.CheckPasswordAsync(created, password));
    }

    [Fact]
    public async Task CopiedLegacyHashIsBlockedEvenWithDifferentIdentityAndNoAdminRole()
    {
        await using var fixture = await Fixture.CreateAsync(migrate: true);
        var legacy = await fixture.Users.FindByIdAsync("admin-user");
        Assert.NotNull(legacy);
        var copiedHash = legacy.PasswordHash;
        Assert.True((await fixture.Users.DeleteAsync(legacy)).Succeeded);
        Assert.True((await fixture.Users.CreateAsync(new IdentityUser
        {
            UserName = "copy@example.com", PasswordHash = copiedHash
        })).Succeeded);

        Assert.False(await fixture.Guard.IsSafeAsync());
    }

    [Fact]
    public async Task WebStartupRefusesUninitializedDatabaseWithoutCreatingSchema()
    {
        await using var connection = new SqliteConnection(
            $"Data Source=uninitialized-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        Assert.Equal(1, await Program.Main([
            "--ConnectionStrings:DefaultConnection", connection.ConnectionString,
            "--environment", "Production"]));

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'";
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task CreateAddsNewAdministratorWithValidatedPassword()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.True((await fixture.Manager.CreateAsync("new@example.com", fixture.NewPassword)).Succeeded);
        var user = await fixture.Users.FindByEmailAsync("new@example.com");
        Assert.NotNull(user);
        Assert.True(await fixture.Users.IsInRoleAsync(user, "Admin"));
        Assert.True(await fixture.Users.CheckPasswordAsync(user, fixture.NewPassword));
    }

    [Fact]
    public async Task CreateRejectsExistingUserWithoutPromotionOrPasswordChange()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = await fixture.AddUserAsync(admin: false);
        var hash = user.PasswordHash;
        var stamp = user.SecurityStamp;
        Assert.False((await fixture.Manager.CreateAsync(user.Email!, fixture.NewPassword)).Succeeded);
        Assert.Equal(hash, user.PasswordHash);
        Assert.Equal(stamp, user.SecurityStamp);
        Assert.False(await fixture.Users.IsInRoleAsync(user, "Admin"));
    }

    [Fact]
    public async Task CreateRejectsExistingAdministratorWithoutOverwritingCredentials()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = await fixture.AddUserAsync(admin: true);
        var hash = user.PasswordHash;
        var stamp = user.SecurityStamp;
        Assert.False((await fixture.Manager.CreateAsync(user.Email!.ToUpperInvariant(), fixture.NewPassword)).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        var unchanged = await fixture.Users.FindByIdAsync(user.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(hash, unchanged.PasswordHash);
        Assert.Equal(stamp, unchanged.SecurityStamp);
        Assert.True(await fixture.Users.IsInRoleAsync(unchanged, "Admin"));
    }

    [Fact]
    public async Task CreateRejectsExistingUsernameEvenWithDifferentEmail()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = new IdentityUser { UserName = "taken@example.com", Email = "other@example.com" };
        Assert.True((await fixture.Users.CreateAsync(user, fixture.OriginalPassword)).Succeeded);
        Assert.False((await fixture.Manager.CreateAsync("taken@example.com", fixture.NewPassword)).Succeeded);
        Assert.False(await fixture.Users.IsInRoleAsync(user, "Admin"));
    }

    [Fact]
    public async Task CreateRejectsWeakPasswordWithoutSavingUserOrRole()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.False((await fixture.Manager.CreateAsync("new@example.com", "weak")).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        Assert.Empty(await fixture.Database.Users.ToListAsync());
        Assert.Empty(await fixture.Database.Roles.ToListAsync());
    }

    [Fact]
    public async Task RoleCreationFailureRollsBackNewUser()
    {
        await using var fixture = await Fixture.CreateAsync(rejectRole: true);
        Assert.False((await fixture.Manager.CreateAsync("new@example.com", fixture.NewPassword)).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        Assert.Empty(await fixture.Database.Users.ToListAsync());
        Assert.Empty(await fixture.Database.Roles.ToListAsync());
    }

    [Fact]
    public async Task MembershipFailureRollsBackNewUserAndRole()
    {
        await using var fixture = await Fixture.CreateAsync(rejectMembership: true);
        Assert.False((await fixture.Manager.CreateAsync("new@example.com", fixture.NewPassword)).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        Assert.Empty(await fixture.Database.Users.ToListAsync());
        Assert.Empty(await fixture.Database.Roles.ToListAsync());
        Assert.Empty(await fixture.Database.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task RotationPreservesIdentityAndMembershipAndChangesStamp()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = await fixture.AddUserAsync(admin: true);
        var id = user.Id;
        var stamp = user.SecurityStamp;
        Assert.True((await fixture.Manager.RotatePasswordAsync(id, fixture.NewPassword)).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        var updated = await fixture.Users.FindByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal(id, updated.Id);
        Assert.NotEqual(stamp, updated.SecurityStamp);
        Assert.True(await fixture.Users.IsInRoleAsync(updated, "Admin"));
        Assert.True(await fixture.Users.CheckPasswordAsync(updated, fixture.NewPassword));
        Assert.False(await fixture.Users.CheckPasswordAsync(updated, fixture.OriginalPassword));
        Assert.Single(await fixture.Database.Users.ToListAsync());
        Assert.Single(await fixture.Database.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task RotationRejectsNonAdministratorWithoutChanges()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = await fixture.AddUserAsync(admin: false);
        var hash = user.PasswordHash;
        var stamp = user.SecurityStamp;
        Assert.False((await fixture.Manager.RotatePasswordAsync(user.Id, fixture.NewPassword)).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        var unchanged = await fixture.Users.FindByIdAsync(user.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(hash, unchanged.PasswordHash);
        Assert.Equal(stamp, unchanged.SecurityStamp);
        Assert.Empty(await fixture.Database.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task RotationRejectsWeakPasswordWithoutChangingHashStampOrMembership()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = await fixture.AddUserAsync(admin: true);
        var hash = user.PasswordHash;
        var stamp = user.SecurityStamp;
        Assert.False((await fixture.Manager.RotatePasswordAsync(user.Id, "weak")).Succeeded);
        fixture.Database.ChangeTracker.Clear();
        var unchanged = await fixture.Users.FindByIdAsync(user.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(hash, unchanged.PasswordHash);
        Assert.Equal(stamp, unchanged.SecurityStamp);
        Assert.True(await fixture.Users.IsInRoleAsync(unchanged, "Admin"));
    }

    [Fact]
    public async Task RotationRejectsMissingUser()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.False((await fixture.Manager.RotatePasswordAsync("missing", fixture.NewPassword)).Succeeded);
        Assert.Empty(await fixture.Database.Users.ToListAsync());
    }

    [Theory]
    [InlineData()]
    [InlineData("admin")]
    [InlineData("admin", "unknown", "target")]
    [InlineData("admin", "create-admin", "target", "password-argument")]
    [InlineData("admin", "rotate-existing-admin-password", " ")]
    public void ParserRejectsMalformedCommandsAndExtraArguments(params string[] args) =>
        Assert.False(AdminCommand.TryParse(args, out _));

    [Theory]
    [InlineData("create-admin")]
    [InlineData("rotate-existing-admin-password")]
    public void ParserAcceptsOnlyExplicitOperations(string operation) =>
        Assert.True(AdminCommand.TryParse(["admin", operation, "target"], out _));

    private sealed class RejectRoleValidator : IRoleValidator<IdentityRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<IdentityRole> manager, IdentityRole role) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "TestRoleRejected" }));
    }

    private sealed class RejectMembershipValidator(ApplicationDbContext database) : IUserValidator<IdentityUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user) =>
            Task.FromResult(database.ChangeTracker.Entries<IdentityUserRole<string>>()
                .Any(entry => entry.State == EntityState.Added)
                    ? IdentityResult.Failed(new IdentityError { Code = "TestMembershipRejected" })
                    : IdentityResult.Success);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ServiceProvider provider;
        private readonly IServiceScope scope;
        public ApplicationDbContext Database { get; }
        public UserManager<IdentityUser> Users { get; }
        public AdministratorManager Manager { get; }
        public LegacyAdministratorStartupGuard Guard { get; }
        public string ConnectionString => connection.ConnectionString;
        public string OriginalPassword { get; } = GenerateTestPassword();
        public string NewPassword { get; }

        private Fixture(SqliteConnection connection, ServiceProvider provider)
        {
            NewPassword = GenerateTestPassword(OriginalPassword);
            this.connection = connection;
            this.provider = provider;
            scope = provider.CreateScope();
            Database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Manager = scope.ServiceProvider.GetRequiredService<AdministratorManager>();
            Guard = scope.ServiceProvider.GetRequiredService<LegacyAdministratorStartupGuard>();
        }

        public static async Task<Fixture> CreateAsync(bool rejectRole = false, bool rejectMembership = false,
            bool migrate = false)
        {
            var connection = new SqliteConnection($"Data Source=admin-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
            services.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            if (rejectRole)
                services.AddScoped<IRoleValidator<IdentityRole>, RejectRoleValidator>();
            if (rejectMembership)
                services.AddScoped<IUserValidator<IdentityUser>, RejectMembershipValidator>();
            services.AddScoped<AdministratorManager>();
            services.AddScoped<LegacyAdministratorStartupGuard>();
            var fixture = new Fixture(connection, services.BuildServiceProvider());
            // Both paths affect only this disposable, uniquely named in-memory database.
            if (migrate)
                await fixture.Database.Database.MigrateAsync();
            else
                await fixture.Database.Database.EnsureCreatedAsync();
            return fixture;
        }

        public async Task<IdentityUser> AddUserAsync(bool admin)
        {
            var user = new IdentityUser { UserName = "existing@example.com", Email = "existing@example.com" };
            Assert.True((await Users.CreateAsync(user, OriginalPassword)).Succeeded);
            if (admin)
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
                Assert.True((await Users.AddToRoleAsync(user, "Admin")).Succeeded);
            }
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            scope.Dispose();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
