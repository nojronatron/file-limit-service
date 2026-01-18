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

### Manual Execution

```bash
# Using command-line arguments
FileLimitService <target-directory> <max-file-count> [noui]

# Using configuration file
FileLimitService --config <config-file-path> [noui]
```

### Arguments

- `target-directory`: Path to the directory to monitor and clean
- `max-file-count`: Maximum number of files to keep in the directory (non-negative integer)
- `--config`: Path to JSON configuration file
- `noui`: (Optional) Suppress all console output while still logging to file

### Examples

```bash
# Keep only 100 files in the logs directory
FileLimitService /var/log/myapp 100

# On Windows, keep 50 files in a backup directory
FileLimitService "C:\Backups\Daily" 50

# Run silently without console output (useful for automated jobs)
FileLimitService /var/log/myapp 100 noui

# Use configuration file
FileLimitService --config /etc/FileLimitService/config.json
```

### Configuration File Format

Create a JSON configuration file:

```json
{
  "targetDirectory": "/var/log/myapp",
  "maxFileCount": 100,
  "enableLogging": true
}
```

- `targetDirectory`: Path to monitor (required)
- `maxFileCount`: Maximum files to keep (required, non-negative)
- `enableLogging`: Enable console/file logging (optional, default: true)

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

For automated deployment, consider setting up a [systemd service](#systemd-service-linux), [cron job](#example-cron-job-linux), or Windows Task Scheduler to run the service on a schedule.

## Systemd Service (Linux)

FileLimitService can run as a systemd service with automatic timer-based execution.

### System-Wide Installation

1. **Download and extract** the latest release:
   ```bash
   wget https://github.com/nojronatron/file-limit-service/releases/latest/download/FileLimitService-linux-arm64.zip
   unzip FileLimitService-linux-arm64.zip -d file-limit-service/
   cd file-limit-service/
   ```

2. **Run the installation script**:
   ```bash
   sudo ./install-system.sh
   ```

3. **Edit the configuration file**:
   ```bash
   sudo nano /etc/FileLimitService/config.json
   ```
   
   Update `targetDirectory` and `maxFileCount` for your needs.

4. **Test manually** before enabling:
   ```bash
   /usr/local/bin/FileLimitService --config /etc/FileLimitService/config.json
   ```

5. **Enable and start the timer**:
   ```bash
   sudo systemctl enable filelimitservice.timer
   sudo systemctl start filelimitservice.timer
   ```

### Service Management

```bash
# Check timer status
sudo systemctl status filelimitservice.timer

# Check service execution logs
sudo journalctl -u filelimitservice -f

# Stop the timer
sudo systemctl stop filelimitservice.timer

# Disable automatic startup
sudo systemctl disable filelimitservice.timer

# Manually trigger cleanup now
sudo systemctl start filelimitservice.service
```

### Customizing Timer Schedule

The default timer runs every hour. To customize:

```bash
# Edit the timer unit
sudo systemctl edit filelimitservice.timer
```

Add an override in the editor:

```ini
[Timer]
# Run every 2 hours
OnUnitActiveSec=2h
```

**Timer interval options:**
- `1h` = Every hour
- `2h` = Every 2 hours
- `12h` = Twice daily
- `24h` or `1d` = Daily
- `168h` or `7d` = Weekly

Save and reload:

```bash
sudo systemctl daemon-reload
sudo systemctl restart filelimitservice.timer
```

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

- **Systemd**: Timer-based service units (Linux) - [See above](#systemd-service-linux)
- **Cron**: Scheduled jobs (Linux/Unix)
- **Task Scheduler**: Windows automation

### Example Cron Job (Linux)

```cron
# Run every day at 2 AM to keep max 1000 files (silent mode)
0 2 * * * /path/to/FileLimitService /var/log/myapp 1000 noui

# Or use config file
0 2 * * * /usr/local/bin/FileLimitService --config /etc/FileLimitService/config.json noui
```

## Future Development

### Phase 3: User Service Support (Planned)

Enable users to run FileLimitService without sudo for personal directories:

- Install to `~/.local/bin/` with `install-user.sh` script
- User systemd services in `~/.config/systemd/user/`
- Config files in `~/.config/FileLimitService/config.json`
- Use `loginctl enable-linger` for background operation after logout
- Ideal for cleaning `~/Downloads`, `~/tmp`, and other user-owned directories

**Benefits:**
- No root privileges required
- Each user manages their own cleanup rules
- Isolated from system-wide configuration

**Limitations:**
- Cannot access system directories like `/var/log`
- Service stops when user logs out (unless linger enabled)

### Phase 4: Multi-Directory Support (Planned)

Monitor multiple directories from a single configuration:

**Config schema:**
```json
{
  "targets": [
    {"path": "/var/log/app1", "maxFiles": 100},
    {"path": "/var/log/app2", "maxFiles": 50},
    {"path": "/tmp/cache", "maxFiles": 200}
  ],
  "enableLogging": true
}
```

**Implementation considerations:**
- Single service execution processes all targets sequentially
- Individual target failures don't stop processing remaining targets
- Aggregate logging with per-target metrics
- Backward compatible with single-directory config format

**Use cases:**
- Centralized cleanup management for multiple applications
- Different retention policies per directory
- Reduced systemd service overhead (one timer instead of many)

## Requirements

- .NET 10 Runtime or SDK
- Read/write permissions on target directory
- Write permissions on log directory

## License

See LICENSE file for details.
