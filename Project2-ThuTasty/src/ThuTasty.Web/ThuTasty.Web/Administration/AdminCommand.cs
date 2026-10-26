namespace ThuTasty.Web.Administration;

public static class AdminCommand
{
    public const string Usage = "Usage: admin create-admin <email> | admin rotate-existing-admin-password <user-id>. Passwords are entered privately at the terminal.";

    public static bool TryParse(string[] args, out string operation)
    {
        operation = string.Empty;
        if (args.Length != 3 || args[0] != "admin" || string.IsNullOrWhiteSpace(args[2]) ||
            (args[1] != "create-admin" && args[1] != "rotate-existing-admin-password"))
            return false;

        operation = args[1];
        return true;
    }

    public static async Task<int> RunAsync(string[] args, AdministratorManager manager)
    {
        if (!TryParse(args, out var operation))
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Administrator commands require an interactive local terminal.");
            return 2;
        }

        try
        {
            Console.WriteLine(operation == "create-admin"
                ? "Create a NEW administrator. Existing accounts will be rejected."
                : "Rotate an EXISTING administrator password. Existing sessions become invalid when their security stamp is next checked.");
            var password = ReadPassword("New password: ");
            var confirmation = ReadPassword("Confirm new password: ");
            if (password != confirmation)
            {
                Console.Error.WriteLine("Passwords do not match. No changes were made.");
                return 1;
            }

            var result = operation == "create-admin"
                ? await manager.CreateAsync(args[2], password)
                : await manager.RotatePasswordAsync(args[2], password);
            if (!result.Succeeded)
            {
                // Descriptions from custom validators can contain input. Print only known-safe text.
                Console.Error.WriteLine("Operation rejected. The target must be eligible and the password must satisfy the configured Identity policy. No changes were saved.");
                return 1;
            }

            Console.WriteLine("Administrator operation completed successfully.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Cancelled. No changes were saved.");
            return 1;
        }
        catch (Exception)
        {
            // Do not print exceptions: database/provider diagnostics can contain sensitive values.
            Console.Error.WriteLine("Administrator operation failed. Check database availability and schema with your normal maintenance tools. No automatic migration was attempted.");
            return 1;
        }
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var buffer = new char[1024];
        var length = 0;
        try
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                    throw new OperationCanceledException();
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return new string(buffer, 0, length);
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (length > 0)
                        buffer[--length] = '\0';
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    if (length == buffer.Length)
                        throw new OperationCanceledException();
                    buffer[length++] = key.KeyChar;
                }
            }
        }
        finally
        {
            Array.Clear(buffer);
        }
    }
}
