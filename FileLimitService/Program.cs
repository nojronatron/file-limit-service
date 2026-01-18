using FileLimitService;

// Parse command line arguments
bool useConfigFile = false;
string? configPath = null;
string? targetDirectory = null;
int maxFileCount = 0;
bool noUi = false;

if (args.Length >= 2 && args[0] == "--config")
{
    // Config file mode: --config <path>
    useConfigFile = true;
    configPath = args[1];
    
    // Check for noui flag after config path
    if (args.Length >= 3 && args[2].Equals("noui", StringComparison.OrdinalIgnoreCase))
    {
        noUi = true;
    }
}
else if (args.Length >= 2)
{
    // Traditional CLI mode: <directory> <count> [noui]
    targetDirectory = args[0];
    if (!int.TryParse(args[1], out maxFileCount) || maxFileCount < 0)
    {
        Console.WriteLine("Error: max-file-count must be a non-negative integer");
        return 1;
    }
    
    if (!Directory.Exists(targetDirectory))
    {
        Console.WriteLine($"Error: Directory '{targetDirectory}' does not exist");
        return 1;
    }
    
    // Check for noui flag
    if (args.Length >= 3 && args[2].Equals("noui", StringComparison.OrdinalIgnoreCase))
    {
        noUi = true;
    }
}
else
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  FileLimitService <target-directory> <max-file-count> [noui]");
    Console.WriteLine("  FileLimitService --config <config-file-path> [noui]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  target-directory: Path to the directory to monitor");
    Console.WriteLine("  max-file-count: Maximum number of files to keep in the directory");
    Console.WriteLine("  --config: Path to JSON configuration file");
    Console.WriteLine("  noui: (Optional) Suppress all console output");
    return 1;
}

try
{
    // Load configuration from file if using config mode
    if (useConfigFile)
    {
        var config = ConfigurationService.LoadFromFile(configPath!);
        targetDirectory = config.TargetDirectory;
        maxFileCount = config.MaxFileCount;
        
        // Config file can override logging preference
        if (!config.EnableLogging)
        {
            noUi = true;
        }
    }
    
    // Create logger and service
    ILogger logger = noUi ? new NullLogger() : new FileLogger();
    var service = new FileCleanupService(logger);
    await service.CleanupDirectoryAsync(targetDirectory!, maxFileCount);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
