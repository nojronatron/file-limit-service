namespace FileLimitService.Tests;

public class FileCleanupServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _logDirectory;

    public FileCleanupServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileLimitService_Test_{Guid.NewGuid()}");
        _logDirectory = Path.Combine(Path.GetTempPath(), $"FileLimitService_Logs_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_logDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        if (Directory.Exists(_logDirectory))
        {
            Directory.Delete(_logDirectory, true);
        }
    }

    [Fact]
    public async Task CleanupDirectoryAsync_WithFewerFilesThanMax_DeletesNoFiles()
    {
        // Arrange
        CreateTestFiles(5);
        var logPath = Path.Combine(_logDirectory, "test.log");
        var logger = new FileLogger(logPath);
        var service = new FileCleanupService(logger);

        // Act
        await service.CleanupDirectoryAsync(_testDirectory, 10);

        // Assert
        Assert.Equal(5, Directory.GetFiles(_testDirectory).Length);
    }

    [Fact]
    public async Task CleanupDirectoryAsync_WithMoreFilesThanMax_DeletesOldestFiles()
    {
        // Arrange
        var files = CreateTestFiles(10);
        var logPath = Path.Combine(_logDirectory, "test.log");
        var logger = new FileLogger(logPath);
        var service = new FileCleanupService(logger);

        // Act
        await service.CleanupDirectoryAsync(_testDirectory, 5);

        // Assert
        var remainingFiles = Directory.GetFiles(_testDirectory);
        Assert.Equal(5, remainingFiles.Length);

        // The 5 newest files should remain
        foreach (var file in files.TakeLast(5))
        {
            Assert.Contains(remainingFiles, f => Path.GetFileName(f) == Path.GetFileName(file));
        }
    }

    [Fact]
    public async Task CleanupDirectoryAsync_WithEmptyDirectory_CompletesSuccessfully()
    {
        // Arrange
        var logPath = Path.Combine(_logDirectory, "test.log");
        var logger = new FileLogger(logPath);
        var service = new FileCleanupService(logger);

        // Act & Assert
        await service.CleanupDirectoryAsync(_testDirectory, 5);
        Assert.Empty(Directory.GetFiles(_testDirectory));
    }

    [Fact]
    public async Task CleanupDirectoryAsync_DeletesExactNumberOfFiles()
    {
        // Arrange
        CreateTestFiles(20);
        var logPath = Path.Combine(_logDirectory, "test.log");
        var logger = new FileLogger(logPath);
        var service = new FileCleanupService(logger);

        // Act
        await service.CleanupDirectoryAsync(_testDirectory, 12);

        // Assert
        Assert.Equal(12, Directory.GetFiles(_testDirectory).Length);
    }

    [Fact]
    public async Task CleanupDirectoryAsync_LogsAllRequiredInformation()
    {
        // Arrange
        CreateTestFiles(5);
        var logPath = Path.Combine(_logDirectory, "test.log");
        var logger = new FileLogger(logPath);
        var service = new FileCleanupService(logger);

        // Act
        await service.CleanupDirectoryAsync(_testDirectory, 3);

        // Assert
        var logContents = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Target Directory:", logContents);
        Assert.Contains("Max File Count:", logContents);
        Assert.Contains("Current file count:", logContents);
        Assert.Contains("Oldest file age:", logContents);
        Assert.Contains("Newest file age:", logContents);
        Assert.Contains("Files deleted:", logContents);
    }

    [Fact]
    public async Task FileLogger_CreatesLogFileWithContent()
    {
        // Arrange
        var logPath = Path.Combine(_logDirectory, "logger_test.log");
        var logger = new FileLogger(logPath);

        // Act
        await logger.LogAsync("Test message");

        // Assert
        Assert.True(File.Exists(logPath));
        var contents = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Test message", contents);
    }

    private List<string> CreateTestFiles(int count)
    {
        var files = new List<string>();
        var baseTime = DateTime.Now;
        
        for (int i = 0; i < count; i++)
        {
            string filePath = Path.Combine(_testDirectory, $"testfile_{i:D3}.txt");
            File.WriteAllText(filePath, $"Test content {i}");
            
            // Set precise timestamps with millisecond granularity to ensure proper ordering
            // Each file gets a timestamp 1 millisecond apart, starting from oldest
            var fileInfo = new FileInfo(filePath);
            fileInfo.LastWriteTime = baseTime.AddMilliseconds(-count + i);
            
            files.Add(filePath);
        }
        return files;
    }
}

