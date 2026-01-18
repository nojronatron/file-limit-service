using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileLimitService;

public class Configuration
{
    public string TargetDirectory { get; set; } = string.Empty;
    public int MaxFileCount { get; set; }
    public bool EnableLogging { get; set; } = true;
}

[JsonSerializable(typeof(Configuration))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
internal partial class ConfigurationJsonContext : JsonSerializerContext
{
}

public class ConfigurationService
{
    public static Configuration LoadFromFile(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        string json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize(json, ConfigurationJsonContext.Default.Configuration);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to parse configuration file: {configPath}");
        }

        ValidateConfiguration(config);
        return config;
    }

    private static void ValidateConfiguration(Configuration config)
    {
        if (string.IsNullOrWhiteSpace(config.TargetDirectory))
        {
            throw new InvalidOperationException("Configuration error: targetDirectory cannot be empty");
        }

        if (!Directory.Exists(config.TargetDirectory))
        {
            throw new DirectoryNotFoundException($"Configuration error: Directory '{config.TargetDirectory}' does not exist");
        }

        if (config.MaxFileCount < 0)
        {
            throw new InvalidOperationException($"Configuration error: maxFileCount must be non-negative (got {config.MaxFileCount})");
        }
    }
}
