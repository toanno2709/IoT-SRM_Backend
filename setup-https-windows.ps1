# HTTPS Certificate Setup Script for Windows
# Run this in PowerShell as Administrator

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "IoT Showroom - HTTPS Certificate Setup (Windows)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "??  This script must be run as Administrator" -ForegroundColor Yellow
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Configuration
$AppDir = "C:\Users\DK\Documents\IoT-SRM_Backend\AppBackend.ApiCore"
$CertDir = Join-Path $AppDir "https"
$CertFile = Join-Path $CertDir "certificate.pfx"
$CertPassword = "IoTShowroom2024!"

Write-Host "?? Application directory: $AppDir" -ForegroundColor White
Write-Host "?? Certificate directory: $CertDir" -ForegroundColor White
Write-Host ""

# Create https directory if it doesn't exist
if (-not (Test-Path $CertDir)) {
    Write-Host "Creating certificate directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $CertDir -Force | Out-Null
}

# Clean existing dev certificates
Write-Host "?? Cleaning existing development certificates..." -ForegroundColor Yellow
dotnet dev-certs https --clean

# Generate new certificate
Write-Host "?? Generating new SSL certificate..." -ForegroundColor Yellow
dotnet dev-certs https -ep $CertFile -p $CertPassword

# Trust the certificate
Write-Host "? Trusting certificate..." -ForegroundColor Yellow
dotnet dev-certs https --trust

# Verify certificate was created
if (Test-Path $CertFile) {
    Write-Host "? Certificate generated successfully!" -ForegroundColor Green
    Write-Host "?? Certificate location: $CertFile" -ForegroundColor Green
    Write-Host ""
    
    # Show file size
    $certInfo = Get-Item $CertFile
    Write-Host "?? Certificate size: $($certInfo.Length) bytes" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "? Certificate generation failed!" -ForegroundColor Red
    exit 1
}

# Check Windows Firewall rules
Write-Host "?? Checking Windows Firewall..." -ForegroundColor Yellow
$httpRule = Get-NetFirewallRule -DisplayName "IoT Showroom HTTP" -ErrorAction SilentlyContinue
$httpsRule = Get-NetFirewallRule -DisplayName "IoT Showroom HTTPS" -ErrorAction SilentlyContinue

if (-not $httpRule) {
    Write-Host "Adding firewall rule for HTTP (8080)..." -ForegroundColor Yellow
    New-NetFirewallRule -DisplayName "IoT Showroom HTTP" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow | Out-Null
}

if (-not $httpsRule) {
    Write-Host "Adding firewall rule for HTTPS (8443)..." -ForegroundColor Yellow
    New-NetFirewallRule -DisplayName "IoT Showroom HTTPS" -Direction Inbound -LocalPort 8443 -Protocol TCP -Action Allow | Out-Null
}

Write-Host "? Firewall configured" -ForegroundColor Green
Write-Host ""

# Test certificate
Write-Host "?? Testing certificate..." -ForegroundColor Yellow
$certCheck = dotnet dev-certs https --check
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Certificate is valid" -ForegroundColor Green
} else {
    Write-Host "??  Certificate validation warning (normal for self-signed)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "? HTTPS Setup Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "?? Next steps:" -ForegroundColor Cyan
Write-Host "1. Run your application:" -ForegroundColor White
Write-Host "   cd $AppDir" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Test the endpoints:" -ForegroundColor White
Write-Host "   HTTP:  http://localhost:8080/api/health" -ForegroundColor Gray
Write-Host "   HTTPS: https://localhost:8443/api/health" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Update frontend API base URL:" -ForegroundColor White
Write-Host "   For local: https://localhost:8443" -ForegroundColor Gray
Write-Host "   For server: https://103.38.236.128:8443" -ForegroundColor Gray
Write-Host ""
Write-Host "??  Certificate password: $CertPassword" -ForegroundColor Yellow
Write-Host ""
