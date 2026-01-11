# FileLimitService

A .NET 10 C# console application that manages file count in a directory by removing the oldest files when the count exceeds a specified threshold.

## Features

- **Cross-platform**: Targets Windows (x64) and Linux (ARM64)
- **Asynchronous Operation**: Efficient file operations using async/await
- **Comprehensive Logging**: Platform-compatible logging to local directories
  - Windows: `%LOCALAPPDATA%\FileLimitService\Logs`
  - Linux: `/var/log/FileLimitService` or `~/.local/share/FileLimitService/logs`
- **Detailed Metrics**: Logs include file counts, age statistics, and deletion details

## Usage

```bash
FileLimitService <target-directory> <max-file-count> [noui]
```

### Arguments

- `target-directory`: Path to the directory to monitor and clean
- `max-file-count`: Maximum number of files to keep in the directory (non-negative integer)
- `noui`: (Optional) Suppress all console output while still logging to file

### Examples

```bash
# Keep only 100 files in the logs directory
FileLimitService /var/log/myapp 100

# On Windows, keep 50 files in a backup directory
FileLimitService "C:\Backups\Daily" 50

# Run silently without console output (useful for automated jobs)
FileLimitService /var/log/myapp 100 noui
```

## Building

### Build for all platforms

```bash
dotnet build FileLimitService.slnx
```

### Publish for Windows (x64)

```bash
dotnet publish FileLimitService/FileLimitService.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64
```

### Publish for Linux (ARM64)

```bash
dotnet publish FileLimitService/FileLimitService.csproj -c Release -r linux-arm64 --self-contained false -o publish/linux-arm64
```

### Installing from GitHub Releases

To download and use pre-built binaries from GitHub Releases:

```bash
# Download the latest release for Linux ARM64
wget https://github.com/nojronatron/file-limit-service/releases/latest/download/FileLimitService-linux-arm64.zip

# Extract the archive
unzip FileLimitService-linux-arm64.zip -d file-limit-service/

# Make the binary executable
chmod +x file-limit-service/FileLimitService

# Run the service
./file-limit-service/FileLimitService /path/to/target 100
```

For automated deployment, consider setting up a [cron job](#example-cron-job-linux) to run the service on a schedule.

## Running Tests

```bash
dotnet test
```

## How It Works

1. **Validation**: Checks that the target directory exists and arguments are valid
2. **Enumeration**: Gets all files in the target directory
3. **Analysis**: Calculates current file count, oldest and newest file ages
4. **Cleanup**: If file count exceeds threshold, deletes oldest files first
5. **Logging**: Records all actions with timestamps to platform-specific log directory

## Logging

The service logs the following information:

- Target directory and maximum file count parameters
- Current file count before cleanup
- Oldest and newest file ages (in days)
- Each deleted file with its age
- Total count of deleted files
- Final file count after cleanup

Logs are written to both the console and a timestamped log file.

## Automation

The service can be automated using:

- **Linux**: Cron jobs
- **Windows**: Task Scheduler
- **Systemd**: Service units (Linux)

### Example Cron Job (Linux)

```cron
# Run every day at 2 AM to keep max 1000 files (silent mode)
0 2 * * * /path/to/FileLimitService /var/log/myapp 1000 noui
```

## Requirements

- .NET 10 Runtime or SDK
- Read/write permissions on target directory
- Write permissions on log directory

## License

See LICENSE file for details.
