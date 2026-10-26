using Microsoft.AspNetCore.Identity;
using ThuTasty.Web.Data;

namespace ThuTasty.Web.Administration;

// Used only by the local operator command, never by an HTTP endpoint or startup seeder.
public sealed class AdministratorManager(
    UserManager<IdentityUser> users,
    RoleManager<IdentityRole> roles,
    ApplicationDbContext database)
{
    public const string AdminRole = "Admin";

    public async Task<IdentityResult> CreateAsync(string email, string password)
    {
        // All writes share the Identity stores' scoped DbContext. Failure rolls everything back.
        await using var transaction = await database.Database.BeginTransactionAsync();
        if (await users.FindByEmailAsync(email) is not null ||
            await users.FindByNameAsync(email) is not null)
        {
            return Failure("ExistingUser");
        }

        var user = new IdentityUser { UserName = email, Email = email };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            return created;

        if (!await roles.RoleExistsAsync(AdminRole))
        {
            var roleCreated = await roles.CreateAsync(new IdentityRole(AdminRole));
            if (!roleCreated.Succeeded)
                return roleCreated;
        }

        var assigned = await users.AddToRoleAsync(user, AdminRole);
        if (!assigned.Succeeded)
            return assigned;

        await transaction.CommitAsync();
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> RotatePasswordAsync(string userId, string password)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        var user = await users.FindByIdAsync(userId);
        if (user is null)
            return Failure("UserNotFound");
        if (!await users.IsInRoleAsync(user, AdminRole))
            return Failure("NotAdministrator");

        // Identity validates the password, hashes it, and changes the security stamp.
        // The token stays in memory; no user or membership is deleted or recreated.
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var reset = await users.ResetPasswordAsync(user, token, password);
        if (!reset.Succeeded)
            return reset;

        await transaction.CommitAsync();
        return IdentityResult.Success;
    }

    private static IdentityResult Failure(string code) =>
        IdentityResult.Failed(new IdentityError { Code = code });
}
