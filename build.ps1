param(
    [string]$Configuration = "Release",
    [switch]$Pack = $true,
    [switch]$Test,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Cici Build Script" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    dotnet clean -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Build solution
Write-Host "🔨 Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Run tests if requested
if ($Test) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    dotnet test -c $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Pack NuGet packages if requested
if ($Pack) {
    Write-Host "📦 Creating NuGet packages..." -ForegroundColor Yellow
    dotnet pack -c $Configuration --no-build --output ./artifacts
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "✅ Build completed successfully!" -ForegroundColor Green