#!/bin/bash
# Installation script for FileLimitService system-wide deployment
# Requires: sudo privileges, .NET 10 runtime installed

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
INSTALL_DIR="/usr/local/bin"
CONFIG_DIR="/etc/FileLimitService"
SYSTEMD_DIR="/etc/systemd/system"
BINARY_NAME="FileLimitService"

echo -e "${GREEN}FileLimitService System-Wide Installation${NC}"
echo "=========================================="
echo ""

# Check for root privileges
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Error: This script must be run with sudo${NC}"
    echo "Usage: sudo ./install-system.sh"
    exit 1
fi

# Check if binary exists in current directory
if [ ! -f "./${BINARY_NAME}" ]; then
    echo -e "${RED}Error: ${BINARY_NAME} binary not found in current directory${NC}"
    echo "Please run this script from the directory containing the published binary"
    exit 1
fi

# Check for .NET 10 runtime
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}Warning: dotnet command not found${NC}"
    echo "FileLimitService requires .NET 10 runtime to be installed"
    echo "Continue anyway? (y/N)"
    read -r response
    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "Installing FileLimitService..."
echo ""

# Step 1: Copy binary
echo -e "${GREEN}[1/5]${NC} Installing binary to ${INSTALL_DIR}..."
cp "./${BINARY_NAME}" "${INSTALL_DIR}/"
chmod +x "${INSTALL_DIR}/${BINARY_NAME}"

# Step 2: Create configuration directory
echo -e "${GREEN}[2/5]${NC} Creating configuration directory ${CONFIG_DIR}..."
mkdir -p "${CONFIG_DIR}"

# Step 3: Install example config if not exists
if [ ! -f "${CONFIG_DIR}/config.json" ]; then
    echo -e "${GREEN}[3/5]${NC} Installing example configuration..."
    if [ -f "./systemd/config.json.example" ]; then
        cp "./systemd/config.json.example" "${CONFIG_DIR}/config.json"
        echo -e "${YELLOW}      Edit ${CONFIG_DIR}/config.json before enabling the service${NC}"
    else
        # Create minimal config if example not found
        cat > "${CONFIG_DIR}/config.json" << EOF
{
  "targetDirectory": "/var/log/myapp",
  "maxFileCount": 100,
  "enableLogging": true
}
EOF
        echo -e "${YELLOW}      Created default config - Edit before use!${NC}"
    fi
else
    echo -e "${GREEN}[3/5]${NC} Configuration file already exists, skipping..."
fi

# Step 4: Install systemd unit files
echo -e "${GREEN}[4/5]${NC} Installing systemd unit files..."
if [ -f "./systemd/filelimitservice.service" ] && [ -f "./systemd/filelimitservice.timer" ]; then
    cp "./systemd/filelimitservice.service" "${SYSTEMD_DIR}/"
    cp "./systemd/filelimitservice.timer" "${SYSTEMD_DIR}/"
else
    echo -e "${RED}Error: systemd unit files not found${NC}"
    exit 1
fi

# Step 5: Reload systemd
echo -e "${GREEN}[5/5]${NC} Reloading systemd daemon..."
systemctl daemon-reload

echo ""
echo -e "${GREEN}Installation complete!${NC}"
echo ""
echo "Next steps:"
echo "  1. Edit configuration: sudo nano ${CONFIG_DIR}/config.json"
echo "  2. Test manually: ${INSTALL_DIR}/${BINARY_NAME} --config ${CONFIG_DIR}/config.json"
echo "  3. Enable service: sudo systemctl enable filelimitservice.timer"
echo "  4. Start service: sudo systemctl start filelimitservice.timer"
echo "  5. Check status: sudo systemctl status filelimitservice.timer"
echo ""
echo "Timer schedule:"
echo "  Default: Runs every hour"
echo "  To customize: sudo systemctl edit filelimitservice.timer"
echo "  View logs: sudo journalctl -u filelimitservice -f"
echo ""
