Write-Host ""
Write-Host "Starting LoanSuperMarket development environment..." -ForegroundColor Cyan
Write-Host ""

# Start Tailwind watcher
Start-Process powershell -ArgumentList "-NoExit", "-Command", "
cd '$PSScriptRoot\src\LoanSuperMarket.Blazor';
npx tailwindcss -i ./wwwroot/css/tailwind-input.css -o ./wwwroot/css/app.css --watch
"

# Start API
Start-Process powershell -ArgumentList "-NoExit", "-Command", "
cd '$PSScriptRoot';
dotnet run --project src/LoanSuperMarket.Api --launch-profile https
"

# Start Blazor
Start-Process powershell -ArgumentList "-NoExit", "-Command", "
cd '$PSScriptRoot';
dotnet run --project src/LoanSuperMarket.Blazor
"

Write-Host ""
Write-Host "Development environment started." -ForegroundColor Green
Write-Host ""