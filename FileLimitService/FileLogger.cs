using System.Runtime.InteropServices;

namespace FileLimitService;

public class FileLogger : ILogger
{
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FileLogger()
    {
        string logDirectory = GetPlatformLogDirectory();
        Directory.CreateDirectory(logDirectory);
        
        string logFileName = $"FileLimitService_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        _logFilePath = Path.Combine(logDirectory, logFileName);
    }

    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        string? directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetPlatformLogDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Use LocalApplicationData
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "FileLimitService", "Logs");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: Use /var/log or user's home directory if no permissions
            string logDir = "/var/log/FileLimitService";
            try
            {
                Directory.CreateDirectory(logDir);
                return logDir;
            }
            catch
            {
                // Fallback to home directory
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(homeDir, ".local", "share", "FileLimitService", "logs");
            }
        }
        else
        {
            // Other platforms: Use user's home directory
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".FileLimitService", "logs");
        }
    }

    public async Task LogAsync(string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        
        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logFilePath, logEntry + Environment.NewLine);
            Console.WriteLine(logEntry);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public string GetLogFilePath() => _logFilePath;
}
