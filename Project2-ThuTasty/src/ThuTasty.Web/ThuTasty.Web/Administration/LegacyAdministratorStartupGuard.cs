using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ThuTasty.Web.Data;

namespace ThuTasty.Web.Administration;

// Read-only protection against the exact credential restored by historical migrations.
public sealed class LegacyAdministratorStartupGuard(ApplicationDbContext database)
{
    // SHA-256 of the historical PasswordHash string, not a password or an Identity hash.
    // This fingerprint cannot authenticate a user. A newly salted hash of the same
    // password will differ, so this is a historical-state check, not a password ban.
    private const string HistoricalHashFingerprint =
        "A6D7FEAC432643A8C6DC3A81377AA88283DFFF8ABC56FC4205CC1687C31813EA";

    public async Task<bool> IsSafeAsync()
    {
        // Check every account so renaming the seed user or copying its hash cannot
        // bypass the guard. AsNoTracking avoids keeping loaded credentials in EF's tracker.
        var hashes = database.Users.AsNoTracking()
            .Where(user => user.PasswordHash != null)
            .Select(user => user.PasswordHash!)
            .AsAsyncEnumerable();

        await foreach (var hash in hashes)
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
            if (fingerprint == HistoricalHashFingerprint)
                return false;
        }

        // A missing administrator is provisioned explicitly, never by web startup.
        return true;
    }
}
