namespace FileLimitService;

public class NullLogger : ILogger
{
    public Task LogAsync(string message)
    {
        // No-op: suppress all output
        return Task.CompletedTask;
    }
}
