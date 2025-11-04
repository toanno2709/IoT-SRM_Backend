#!/bin/bash

# HTTPS Certificate Setup Script for IoT Showroom Backend
# Run this on your server: 103.38.236.128

set -e  # Exit on error

echo "=========================================="
echo "IoT Showroom - HTTPS Certificate Setup"
echo "=========================================="
echo ""

# Prompt for application directory
read -p "Enter your application directory path [default: /home/iot-showroom/app]: " input_dir
APP_DIR="${input_dir:-/home/iot-showroom/app}"

# Check if directory exists
if [ ! -d "$APP_DIR" ]; then
    echo "??  Directory $APP_DIR does not exist!"
    read -p "Create it? (y/n): " create_dir
    if [ "$create_dir" = "y" ]; then
        mkdir -p "$APP_DIR"
        echo "? Directory created"
    else
        echo "? Exiting..."
        exit 1
    fi
fi

CERT_DIR="$APP_DIR/https"
CERT_FILE="$CERT_DIR/certificate.pfx"
CERT_PASSWORD="IoTShowroom2024!"

echo "?? Application directory: $APP_DIR"
echo "?? Certificate directory: $CERT_DIR"
echo ""

# Create https directory if it doesn't exist
if [ ! -d "$CERT_DIR" ]; then
    echo "Creating certificate directory..."
    mkdir -p "$CERT_DIR"
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "? .NET SDK is not installed!"
    echo "Installing .NET 9 SDK..."
    
    # Detect OS
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        OS=$ID
        VERSION=$VERSION_ID
    else
        echo "Cannot detect OS. Please install .NET 9 SDK manually."
        exit 1
    fi
    
    # Install .NET 9 SDK
    if [ "$OS" = "ubuntu" ] || [ "$OS" = "debian" ]; then
        # Install prerequisites
        sudo apt-get update
        sudo apt-get install -y wget apt-transport-https
        
        # Download and install .NET 9
        wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh --channel 9.0
        
        # Add to PATH
        export PATH="$PATH:$HOME/.dotnet"
        echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc
        
        rm dotnet-install.sh
    else
        echo "Please install .NET 9 SDK manually for your OS: $OS"
        echo "Visit: https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    fi
else
    echo "? .NET SDK found: $(dotnet --version)"
fi

# Remove old certificate if exists
if [ -f "$CERT_FILE" ]; then
    echo "??  Existing certificate found"
    read -p "Replace it? (y/n): " replace_cert
    if [ "$replace_cert" = "y" ]; then
        rm "$CERT_FILE"
        echo "???  Old certificate removed"
    else
        echo "Keeping existing certificate"
        exit 0
    fi
fi

# Clean existing dev certificates (only for current user)
echo "?? Cleaning existing development certificates..."
dotnet dev-certs https --clean 2>/dev/null || true

# Generate development certificate
echo "?? Generating SSL certificate..."
if dotnet dev-certs https -ep "$CERT_FILE" -p "$CERT_PASSWORD"; then
    echo "? Certificate generated successfully!"
else
    echo "? Certificate generation failed!"
    echo "Trying alternative method..."
    
    # Alternative: Use OpenSSL if dotnet dev-certs fails
    if command -v openssl &> /dev/null; then
        echo "Using OpenSSL to generate certificate..."
        
        # Generate private key
        openssl genrsa -out "$CERT_DIR/server.key" 2048
        
        # Generate certificate
        openssl req -new -x509 -key "$CERT_DIR/server.key" -out "$CERT_DIR/server.crt" -days 365 -subj "/CN=103.38.236.128"
        
        # Convert to PFX
        openssl pkcs12 -export -out "$CERT_FILE" -inkey "$CERT_DIR/server.key" -in "$CERT_DIR/server.crt" -password "pass:$CERT_PASSWORD"
        
        # Cleanup temporary files
        rm "$CERT_DIR/server.key" "$CERT_DIR/server.crt"
        
        echo "? Certificate generated using OpenSSL"
    else
        echo "? OpenSSL not found. Cannot generate certificate."
        exit 1
    fi
fi

# Set proper permissions
echo "?? Setting certificate permissions..."
chmod 600 "$CERT_FILE"

# Change ownership to current user (not root)
if [ "$EUID" -eq 0 ]; then
    # Running as root, set ownership to the directory owner
    dir_owner=$(stat -c '%U' "$APP_DIR")
    dir_group=$(stat -c '%G' "$APP_DIR")
    chown "$dir_owner:$dir_group" "$CERT_FILE"
    echo "?? Certificate owner: $dir_owner:$dir_group"
fi

# Check if certificate was created
if [ -f "$CERT_FILE" ]; then
    cert_size=$(stat -c%s "$CERT_FILE")
    echo "? Certificate file created"
    echo "?? Location: $CERT_FILE"
    echo "?? Size: $cert_size bytes"
    echo ""
else
    echo "? Certificate file not found!"
    exit 1
fi

# Configure firewall (if ufw is installed)
if command -v ufw &> /dev/null; then
    echo "?? Configuring firewall (UFW)..."
    
    # Check if running with sudo
    if [ "$EUID" -eq 0 ]; then
        ufw allow 8080/tcp comment "IoT Showroom HTTP" 2>/dev/null || true
        ufw allow 8443/tcp comment "IoT Showroom HTTPS" 2>/dev/null || true
        echo "? Firewall rules added"
    else
        echo "??  Need sudo to configure firewall. Run these commands manually:"
        echo "   sudo ufw allow 8080/tcp"
        echo "   sudo ufw allow 8443/tcp"
    fi
    echo ""
elif command -v firewall-cmd &> /dev/null; then
    echo "?? Configuring firewall (firewalld)..."
    
    if [ "$EUID" -eq 0 ]; then
        firewall-cmd --permanent --add-port=8080/tcp 2>/dev/null || true
        firewall-cmd --permanent --add-port=8443/tcp 2>/dev/null || true
        firewall-cmd --reload 2>/dev/null || true
        echo "? Firewall rules added"
    else
        echo "??  Need sudo to configure firewall. Run these commands manually:"
        echo "   sudo firewall-cmd --permanent --add-port=8080/tcp"
        echo "   sudo firewall-cmd --permanent --add-port=8443/tcp"
        echo "   sudo firewall-cmd --reload"
    fi
    echo ""
else
    echo "??  No firewall detected. Make sure ports 8080 and 8443 are open."
    echo ""
fi

echo ""
echo "=========================================="
echo "? HTTPS Setup Complete!"
echo "=========================================="
echo ""
echo "?? Next steps:"
echo ""
echo "1??  Restart your application:"
echo "   cd $APP_DIR"
echo "   dotnet run --project AppBackend.ApiCore/AppBackend.ApiCore.csproj"
echo ""
echo "   Or if using systemd:"
echo "   sudo systemctl restart your-app-name"
echo ""
echo "2??  Test the endpoints:"
echo "   HTTP:  curl http://103.38.236.128:8080/api/health"
echo "   HTTPS: curl -k https://103.38.236.128:8443/api/health"
echo ""
echo "3??  Update your frontend to use:"
echo "   https://103.38.236.128:8443"
echo ""
echo "   Example .env file:"
echo "   VITE_API_BASE_URL=https://103.38.236.128:8443"
echo ""
echo "??  IMPORTANT NOTES:"
echo "????????????????????????????????????????"
echo "• This is a self-signed certificate"
echo "• Browsers will show security warnings"
echo "• Users need to click 'Advanced' ? 'Proceed'"
echo "• Certificate password: $CERT_PASSWORD"
echo ""
echo "For production without warnings:"
echo "• Use Let's Encrypt (requires domain name)"
echo "• Or use a reverse proxy (Nginx/Caddy)"
echo "• Or use Cloudflare Tunnel"
echo ""
echo "?? See HTTPS_SETUP_SOLUTION.md for details"
echo ""
