namespace FileLimitService;

public class FileCleanupService
{
    private readonly ILogger _logger;

    public FileCleanupService()
    {
        _logger = new FileLogger();
    }

    public FileCleanupService(ILogger logger)
    {
        _logger = logger;
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalDays >= 1)
            return $"{age.TotalDays:F2} days";
        else if (age.TotalHours >= 1)
            return $"{age.TotalHours:F2} hours";
        else if (age.TotalMinutes >= 1)
            return $"{age.TotalMinutes:F2} minutes";
        else
            return $"{age.TotalSeconds:F2} seconds";
    }

    public async Task CleanupDirectoryAsync(string targetDirectory, int maxFileCount)
    {
        await _logger.LogAsync($"Starting cleanup - Target Directory: {targetDirectory}, Max File Count: {maxFileCount}");

        // Get all files in the directory
        var files = Directory.GetFiles(targetDirectory)
            .Select(f => new FileInfo(f))
            .ToList();

        int startFileCount = files.Count;
        await _logger.LogAsync($"Current file count: {startFileCount}");

        if (files.Count == 0)
        {
            await _logger.LogAsync("No files found in directory");
            return;
        }

        // Calculate min and max file age
        var oldestFile = files.MinBy(f => f.LastWriteTime);
        var newestFile = files.MaxBy(f => f.LastWriteTime);

        if (oldestFile != null && newestFile != null)
        {
            var oldestAge = DateTime.Now - oldestFile.LastWriteTime;
            var newestAge = DateTime.Now - newestFile.LastWriteTime;
            await _logger.LogAsync($"Oldest file age: {FormatAge(oldestAge)}");
            await _logger.LogAsync($"Newest file age: {FormatAge(newestAge)}");
        }

        // Only delete files if count exceeds threshold
        if (files.Count <= maxFileCount)
        {
            await _logger.LogAsync("File count within limit. No files deleted.");
            await _logger.LogAsync($"Files deleted: 0");
            return;
        }

        // Sort files by LastWriteTime (oldest first)
        var sortedFiles = files.OrderBy(f => f.LastWriteTime).ToList();

        // Calculate how many files to delete
        int filesToDelete = files.Count - maxFileCount;
        int deletedCount = 0;

        // Delete oldest files
        for (int i = 0; i < filesToDelete; i++)
        {
            try
            {
                var fileAge = DateTime.Now - sortedFiles[i].LastWriteTime;
                sortedFiles[i].Delete();
                deletedCount++;
                await _logger.LogAsync($"Deleted: {sortedFiles[i].Name} (Age: {FormatAge(fileAge)})");
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Error deleting {sortedFiles[i].Name}: {ex.Message}");
            }
        }

        await _logger.LogAsync($"Files deleted: {deletedCount}");
        await _logger.LogAsync($"Cleanup completed. Remaining files: {startFileCount - deletedCount}");
    }
}
