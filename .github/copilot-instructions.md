# FileLimitService AI Coding Agent Instructions

## Project Overview
A cross-platform .NET 10 console application that enforces file count limits in directories by deleting oldest files. Targets Windows x64 and Linux ARM64 with platform-specific log directory resolution.

## Architecture

**Single-responsibility components:**
- `FileCleanupService`: Core cleanup logic with async file operations
- `ILogger` interface: Abstraction for output control (see [ILogger.cs](FileLimitService/ILogger.cs))
- `FileLogger`: Platform-aware logging with semaphore-based thread safety
- `NullLogger`: Silent operation for automated/scheduled jobs
- `Program.cs`: Minimal entry point with argument parsing and dependency injection

**Key pattern:** Constructor injection of `ILogger` enables testability. See [FileCleanupService.cs](FileLimitService/FileCleanupService.cs#L8-L16) for the dual-constructor pattern supporting both default and injected loggers.

## Platform-Specific Behavior

**Log directory resolution** (see [FileLogger.cs](FileLimitService/FileLogger.cs#L27-L54)):
- Windows: `%LOCALAPPDATA%\FileLimitService\Logs`
- Linux: `/var/log/FileLimitService` (falls back to `~/.local/share/FileLimitService/logs` if permissions denied)
- Uses `RuntimeInformation.IsOSPlatform()` for detection

## Testing Conventions

**xUnit with disposable test fixtures** ([UnitTest1.cs](FileLimitService.Tests/UnitTest1.cs)):
- Create isolated temp directories per test in constructor
- Clean up in `Dispose()` - prevents cross-test pollution
- Pass custom log paths to `FileLogger` constructor to avoid /var/log permission issues
- Tests verify file count AND identity of remaining files (newest files preserved)

**Example pattern:**
```csharp
var logPath = Path.Combine(_logDirectory, "test.log");
var logger = new FileLogger(logPath);
var service = new FileCleanupService(logger);
```

## Build & Publish

Use `.slnx` solution file (new format):
```bash
dotnet build FileLimitService.slnx
dotnet test
```

**Platform-specific publishing:**
```bash
dotnet publish FileLimitService/FileLimitService.csproj -c Release -r win-x64 --self-contained false
dotnet publish FileLimitService/FileLimitService.csproj -c Release -r linux-arm64 --self-contained false
```

Note: `--self-contained false` requires .NET 10 runtime on target system.

**GitHub Releases workflow:**
- Binaries published as platform-specific zip files: `FileLimitService-{runtime}.zip`
- Users download via wget/curl from `https://github.com/nojronatron/file-limit-service/releases/latest/download/`
- Linux binaries require `chmod +x` after extraction
- See README for cron job setup patterns

## Critical Implementation Details

**File age formatting logic** ([FileCleanupService.cs](FileLimitService/FileCleanupService.cs#L18-L27)): Dynamically selects time unit (days/hours/minutes/seconds) for readability in logs.

**Deletion logic** ([FileCleanupService.cs](FileLimitService/FileCleanupService.cs#L61-L77)): Files sorted by `LastWriteTime` ascending, oldest deleted first. Each deletion wrapped in try-catch to continue on errors.

**Exception handling pattern** - Critical for production reliability:
- File I/O operations (delete, permissions, path access) must be wrapped in try-catch
- Log errors with `await _logger.LogAsync($"Error: {ex.Message}")` but continue execution
- Never crash the application on individual file failures
- Example: Deletion loop continues even if single file delete fails

**CLI argument parsing** ([Program.cs](FileLimitService/Program.cs)):
- Third argument `noui` (case-insensitive) enables `NullLogger`
- Validates directory existence BEFORE creating service
- Returns exit codes: 0 for success, 1 for errors

##**All file I/O must use try-catch** - log errors and continue, never crash on individual file failures
- Any new features requiring permissions/paths should follow FileLogger's fallback pattern

- Maintain async/await pattern throughout - all I/O is async
- Preserve platform detection logic in `FileLogger.GetPlatformLogDirectory()`
- Keep `ILogger` interface simple - only `LogAsync` method
- Tests must use isolated temp directories and custom log paths
- Any new file operations should handle exceptions gracefully (log and continue pattern)

## Future Work
- Deployment automation workflows (systemd service, Windows Task Scheduler) not yet documented