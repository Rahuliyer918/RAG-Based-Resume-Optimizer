# build.ps1 — Builds the Next.js frontend and copies output into the ASP.NET Core wwwroot.
#
# Usage (from the project root):
#   .\build.ps1            — production build (uses .env.production, API URL = relative)
#   .\build.ps1 -DevMode   — skip copy step so you can run frontend with `npm run dev`
#
# After running this script, start the API with:
#   cd src\ResumeOptimizer.Api && dotnet run

param(
    [switch]$DevMode
)

$ErrorActionPreference = "Stop"
$Root        = $PSScriptRoot
$FrontendDir = Join-Path $Root "frontend"
$OutDir      = Join-Path $FrontendDir "out"
$WwwrootDir  = Join-Path $Root "src\ResumeOptimizer.Api\wwwroot"

Write-Host ""
Write-Host "==> Building Next.js frontend..." -ForegroundColor Cyan
Push-Location $FrontendDir
try {
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

if ($DevMode) {
    Write-Host ""
    Write-Host "DevMode: skipping copy to wwwroot." -ForegroundColor Yellow
    Write-Host "Run the API with:   cd src\ResumeOptimizer.Api && dotnet run"
    Write-Host "Run the frontend with:  cd frontend && npm run dev"
    exit 0
}

Write-Host ""
Write-Host "==> Copying output to wwwroot..." -ForegroundColor Cyan

# Clear existing wwwroot contents (but keep the directory)
if (Test-Path $WwwrootDir) {
    Get-ChildItem -Path $WwwrootDir | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $WwwrootDir | Out-Null
}

# Copy Next.js static export into wwwroot
Copy-Item -Path (Join-Path $OutDir "*") -Destination $WwwrootDir -Recurse -Force

Write-Host ""
Write-Host "==> Done! Start the API with:" -ForegroundColor Green
Write-Host "    cd src\ResumeOptimizer.Api && dotnet run"
Write-Host ""
Write-Host "    Then open http://localhost:5272 in your browser."
Write-Host ""
