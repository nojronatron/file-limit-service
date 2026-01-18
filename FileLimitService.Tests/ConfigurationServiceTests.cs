using System.Text.Json;

namespace FileLimitService.Tests;

public class ConfigurationServiceTests : IDisposable
{
    private readonly string _testDirectory;

    public ConfigurationServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileLimitService_ConfigTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void LoadFromFile_ValidConfig_ReturnsConfiguration()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var targetDir = Path.Combine(_testDirectory, "target");
        Directory.CreateDirectory(targetDir);

        var json = @"{
            ""targetDirectory"": """ + targetDir.Replace("\\", "\\\\") + @""",
            ""maxFileCount"": 100,
            ""enableLogging"": true
        }";
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationService.LoadFromFile(configPath);

        // Assert
        Assert.Equal(targetDir, config.TargetDirectory);
        Assert.Equal(100, config.MaxFileCount);
        Assert.True(config.EnableLogging);
    }

    [Fact]
    public void LoadFromFile_ConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigurationService.LoadFromFile(configPath));
    }

    [Fact]
    public void LoadFromFile_EmptyTargetDirectory_ThrowsInvalidOperationException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var json = @"{
            ""targetDirectory"": """",
            ""maxFileCount"": 100
        }";
        File.WriteAllText(configPath, json);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigurationService.LoadFromFile(configPath));
        Assert.Contains("targetDirectory cannot be empty", ex.Message);
    }

    [Fact]
    public void LoadFromFile_DirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var json = @"{
            ""targetDirectory"": ""/nonexistent/directory"",
            ""maxFileCount"": 100
        }";
        File.WriteAllText(configPath, json);

        // Act & Assert
        var ex = Assert.Throws<DirectoryNotFoundException>(() => ConfigurationService.LoadFromFile(configPath));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void LoadFromFile_NegativeMaxFileCount_ThrowsInvalidOperationException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var targetDir = Path.Combine(_testDirectory, "target");
        Directory.CreateDirectory(targetDir);

        var json = @"{
            ""targetDirectory"": """ + targetDir.Replace("\\", "\\\\") + @""",
            ""maxFileCount"": -5
        }";
        File.WriteAllText(configPath, json);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigurationService.LoadFromFile(configPath));
        Assert.Contains("maxFileCount must be non-negative", ex.Message);
    }

    [Fact]
    public void LoadFromFile_CaseInsensitiveProperties_ParsesCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var targetDir = Path.Combine(_testDirectory, "target");
        Directory.CreateDirectory(targetDir);

        var json = @"{
            ""TARGETDIRECTORY"": """ + targetDir.Replace("\\", "\\\\") + @""",
            ""MaxFileCount"": 50,
            ""enablelogging"": false
        }";
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationService.LoadFromFile(configPath);

        // Assert
        Assert.Equal(targetDir, config.TargetDirectory);
        Assert.Equal(50, config.MaxFileCount);
        Assert.False(config.EnableLogging);
    }

    [Fact]
    public void LoadFromFile_WithCommentsAndTrailingCommas_ParsesCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var targetDir = Path.Combine(_testDirectory, "target");
        Directory.CreateDirectory(targetDir);

        var json = @"{
            // This is a comment
            ""targetDirectory"": """ + targetDir.Replace("\\", "\\\\") + @""",
            ""maxFileCount"": 100, // Another comment
            ""enableLogging"": true,
        }";
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationService.LoadFromFile(configPath);

        // Assert
        Assert.Equal(targetDir, config.TargetDirectory);
        Assert.Equal(100, config.MaxFileCount);
        Assert.True(config.EnableLogging);
    }

    [Fact]
    public void LoadFromFile_MissingEnableLogging_DefaultsToTrue()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.json");
        var targetDir = Path.Combine(_testDirectory, "target");
        Directory.CreateDirectory(targetDir);

        var json = @"{
            ""targetDirectory"": """ + targetDir.Replace("\\", "\\\\") + @""",
            ""maxFileCount"": 100
        }";
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationService.LoadFromFile(configPath);

        // Assert
        Assert.True(config.EnableLogging);
    }
}
