namespace FileLimitService;

public interface ILogger
{
    Task LogAsync(string message);
}
