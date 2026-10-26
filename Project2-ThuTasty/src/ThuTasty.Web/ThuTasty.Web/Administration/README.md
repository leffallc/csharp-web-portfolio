# Administrator startup protection

Normal web startup reads the Identity password hashes before opening the HTTP
listener. If any hash matches the exact historical seeded credential, startup
prints a fixed instruction and returns exit code 1. A failed database check also
returns 1. This check does not migrate, create, delete, or update anything.

The guard stores a SHA-256 fingerprint of the historical Identity hash. Think of
it as a checksum used to recognize one exact old value. It is not a password or
an Identity password hash and cannot be used to sign in. Actual stored hashes
are read only into memory and are never included in the guard's messages.
Checking all users prevents renaming the original account or copying its hash
to another account from bypassing the check.

## Provisioning a fresh database

Use the intended environment and database connection consistently for migration,
the operator command, and the website. The commands below use the application's
configured connection. Run them from the web project's directory.

1. As an explicit maintenance action, apply the migrations to the intended fresh
   database (`dotnet ef database update`). This still restores the historical
   seeded administrator because historical migrations remain unchanged.
2. Before starting the website, run this command in an interactive local terminal:

   ```text
   dotnet run --no-launch-profile -- admin rotate-existing-admin-password admin-user
   ```

   Enter a unique new password twice when prompted. The terminal hides input.
   The existing operator command uses Identity to validate and hash the password
   and update its security stamp while preserving the user's ID and Admin role.
3. Start the website normally. The changed password hash passes the startup check.

The operator command runs before the web startup guard and never starts the web
server. Creating an additional administrator does not fix an unchanged legacy
credential: rotate the seeded account's password first.

If the administrator is absent, the guard allows startup without creating one.
An operator can explicitly provision a new account with
`dotnet run --no-launch-profile -- admin create-admin <email>`.
Neither operator command automatically applies migrations.

## Limits

- This recognizes one historical hash, not the old plaintext password. Identity
  uses a random salt, so choosing that same password again produces a different
  hash and is outside this check. Operators must choose unique new passwords.
- The fingerprint is a recognizable marker for historical state, not a secret
  or a defense against an attacker who can edit the database or application.
- This checks once at startup. It does not monitor later database changes or
  replace password policy, access controls, or session invalidation checks.
- Historical migrations still contain the original seed data; they are preserved
  to keep existing database migration history intact.
- Tests use uniquely named in-memory SQLite databases. They never connect to the
  real application database.
