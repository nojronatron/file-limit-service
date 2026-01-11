using FileLimitService;

if (args.Length < 2)
{
    Console.WriteLine("Usage: FileLimitService <target-directory> <max-file-count> [noui]");
    Console.WriteLine("  target-directory: Path to the directory to monitor");
    Console.WriteLine("  max-file-count: Maximum number of files to keep in the directory");
    Console.WriteLine("  noui: (Optional) Suppress all console output");
    return 1;
}

string targetDirectory = args[0];
if (!int.TryParse(args[1], out int maxFileCount) || maxFileCount < 0)
{
    Console.WriteLine("Error: max-file-count must be a non-negative integer");
    return 1;
}

if (!Directory.Exists(targetDirectory))
{
    Console.WriteLine($"Error: Directory '{targetDirectory}' does not exist");
    return 1;
}

// Check for 'noui' argument
bool noUi = args.Length >= 3 && args[2].Equals("noui", StringComparison.OrdinalIgnoreCase);

try
{
    ILogger logger = noUi ? new NullLogger() : new FileLogger();
    var service = new FileCleanupService(logger);
    await service.CleanupDirectoryAsync(targetDirectory, maxFileCount);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
