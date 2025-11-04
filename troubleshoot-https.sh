#!/bin/bash

# HTTPS Troubleshooting Script
# Run this to diagnose HTTPS issues

echo "=========================================="
echo "IoT Showroom - HTTPS Troubleshooting"
echo "=========================================="
echo ""

# Configuration
APP_DIR="/home/iot-showroom/app"  # Update if needed
CERT_FILE="$APP_DIR/https/certificate.pfx"
SERVER_IP="103.38.236.128"
HTTP_PORT="8080"
HTTPS_PORT="8443"

echo "?? Checking configuration..."
echo ""

# 1. Check certificate file
echo "1??  Certificate File:"
if [ -f "$CERT_FILE" ]; then
    echo "   ? Certificate exists: $CERT_FILE"
    cert_size=$(stat -c%s "$CERT_FILE" 2>/dev/null || stat -f%z "$CERT_FILE" 2>/dev/null)
    echo "   ?? Size: $cert_size bytes"
    
    cert_perms=$(stat -c%a "$CERT_FILE" 2>/dev/null || stat -f%A "$CERT_FILE" 2>/dev/null)
    echo "   ?? Permissions: $cert_perms"
    
    if [ "$cert_perms" != "600" ]; then
        echo "   ??  Permissions should be 600"
        echo "   Fix: chmod 600 $CERT_FILE"
    fi
else
    echo "   ? Certificate NOT found: $CERT_FILE"
    echo "   Fix: Run setup-https.sh to generate it"
fi
echo ""

# 2. Check .NET SDK
echo "2??  .NET SDK:"
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    echo "   ? .NET SDK installed: $dotnet_version"
else
    echo "   ? .NET SDK NOT installed"
    echo "   Fix: Install .NET 9 SDK"
fi
echo ""

# 3. Check if application is running
echo "3??  Application Status:"
if pgrep -f "dotnet.*AppBackend.ApiCore" > /dev/null; then
    echo "   ? Application is running"
    
    # Get process details
    pid=$(pgrep -f "dotnet.*AppBackend.ApiCore")
    echo "   ?? PID: $pid"
    
    # Check working directory
    if [ -d "/proc/$pid" ]; then
        cwd=$(readlink -f /proc/$pid/cwd 2>/dev/null)
        echo "   ?? Working directory: $cwd"
    fi
else
    echo "   ??  Application is NOT running"
    echo "   Fix: Start the application"
fi
echo ""

# 4. Check listening ports
echo "4??  Listening Ports:"
http_listening=$(netstat -tlnp 2>/dev/null | grep ":$HTTP_PORT " || ss -tlnp 2>/dev/null | grep ":$HTTP_PORT ")
https_listening=$(netstat -tlnp 2>/dev/null | grep ":$HTTPS_PORT " || ss -tlnp 2>/dev/null | grep ":$HTTPS_PORT ")

if [ -n "$http_listening" ]; then
    echo "   ? HTTP port $HTTP_PORT is listening"
else
    echo "   ? HTTP port $HTTP_PORT is NOT listening"
fi

if [ -n "$https_listening" ]; then
    echo "   ? HTTPS port $HTTPS_PORT is listening"
else
    echo "   ? HTTPS port $HTTPS_PORT is NOT listening"
    echo "   Fix: Check application logs for errors"
fi
echo ""

# 5. Check firewall
echo "5??  Firewall Configuration:"
if command -v ufw &> /dev/null; then
    ufw_status=$(sudo ufw status 2>/dev/null | grep -E "$HTTP_PORT|$HTTPS_PORT" || echo "Not configured")
    echo "   UFW Status:"
    echo "$ufw_status" | sed 's/^/   /'
elif command -v firewall-cmd &> /dev/null; then
    firewall_ports=$(sudo firewall-cmd --list-ports 2>/dev/null || echo "Not configured")
    echo "   Firewalld Status:"
    echo "   $firewall_ports"
else
    echo "   ??  No firewall detected"
fi
echo ""

# 6. Test HTTP endpoint
echo "6??  Testing HTTP Endpoint:"
if command -v curl &> /dev/null; then
    http_test=$(curl -s -o /dev/null -w "%{http_code}" "http://$SERVER_IP:$HTTP_PORT/api/health" --connect-timeout 5 2>/dev/null || echo "000")
    
    if [ "$http_test" = "200" ]; then
        echo "   ? HTTP endpoint is working (Status: $http_test)"
    elif [ "$http_test" = "000" ]; then
        echo "   ? Cannot connect to HTTP endpoint"
        echo "   Fix: Check if application is running and port is open"
    else
        echo "   ??  HTTP endpoint returned: $http_test"
    fi
else
    echo "   ??  curl not installed, skipping test"
fi
echo ""

# 7. Test HTTPS endpoint
echo "7??  Testing HTTPS Endpoint:"
if command -v curl &> /dev/null; then
    https_test=$(curl -s -o /dev/null -w "%{http_code}" -k "https://$SERVER_IP:$HTTPS_PORT/api/health" --connect-timeout 5 2>/dev/null || echo "000")
    
    if [ "$https_test" = "200" ]; then
        echo "   ? HTTPS endpoint is working (Status: $https_test)"
    elif [ "$https_test" = "000" ]; then
        echo "   ? Cannot connect to HTTPS endpoint"
        echo "   Fix: Check certificate and application logs"
    else
        echo "   ??  HTTPS endpoint returned: $https_test"
    fi
    
    # Test certificate details
    echo ""
    echo "   Certificate Details:"
    cert_info=$(echo | openssl s_client -connect $SERVER_IP:$HTTPS_PORT -servername $SERVER_IP 2>/dev/null | openssl x509 -noout -dates 2>/dev/null || echo "Cannot retrieve")
    echo "$cert_info" | sed 's/^/   /'
else
    echo "   ??  curl not installed, skipping test"
fi
echo ""

# 8. Check appsettings.json
echo "8??  Configuration File:"
appsettings_path="$APP_DIR/AppBackend.ApiCore/appsettings.json"
if [ -f "$appsettings_path" ]; then
    echo "   ? appsettings.json found"
    
    # Check certificate path in config
    cert_path_config=$(grep -A2 '"Certificate"' "$appsettings_path" | grep '"Path"' | cut -d'"' -f4)
    echo "   ?? Configured cert path: $cert_path_config"
    
    # Check if HTTPS endpoint is configured
    https_url=$(grep '"Https"' "$appsettings_path" -A1 | grep '"Url"' | cut -d'"' -f4)
    echo "   ?? Configured HTTPS URL: $https_url"
else
    echo "   ??  appsettings.json not found at: $appsettings_path"
fi
echo ""

# 9. Check application logs (if available)
echo "9??  Recent Application Logs:"
log_files=(
    "$APP_DIR/logs/app.log"
    "/var/log/your-app.log"
    "$APP_DIR/AppBackend.ApiCore/bin/Debug/net9.0/logs/app.log"
)

log_found=false
for log_file in "${log_files[@]}"; do
    if [ -f "$log_file" ]; then
        echo "   ?? Found log: $log_file"
        echo "   Last 5 lines:"
        tail -n 5 "$log_file" 2>/dev/null | sed 's/^/   /'
        log_found=true
        break
    fi
done

if [ "$log_found" = false ]; then
    echo "   ??  No log files found in common locations"
    echo "   Check: journalctl -u your-app-name.service"
fi
echo ""

# Summary
echo "=========================================="
echo "?? Summary"
echo "=========================================="
echo ""

issues_found=0

if [ ! -f "$CERT_FILE" ]; then
    echo "? Certificate file missing"
    ((issues_found++))
fi

if ! pgrep -f "dotnet.*AppBackend.ApiCore" > /dev/null; then
    echo "? Application not running"
    ((issues_found++))
fi

if [ -z "$https_listening" ]; then
    echo "? HTTPS port not listening"
    ((issues_found++))
fi

if [ "$https_test" != "200" ] && [ "$https_test" != "" ]; then
    echo "? HTTPS endpoint not responding correctly"
    ((issues_found++))
fi

if [ $issues_found -eq 0 ]; then
    echo "? No issues detected!"
    echo ""
    echo "Your backend should be accessible at:"
    echo "  • HTTP:  http://$SERVER_IP:$HTTP_PORT"
    echo "  • HTTPS: https://$SERVER_IP:$HTTPS_PORT"
    echo ""
    echo "Update your frontend to use: https://$SERVER_IP:$HTTPS_PORT"
else
    echo "??  Found $issues_found issue(s)"
    echo ""
    echo "Common fixes:"
    echo "  1. Generate certificate: bash setup-https.sh"
    echo "  2. Start application: dotnet run"
    echo "  3. Check logs: journalctl -u your-app-name -n 50"
    echo "  4. Check firewall: sudo ufw status"
fi
echo ""
