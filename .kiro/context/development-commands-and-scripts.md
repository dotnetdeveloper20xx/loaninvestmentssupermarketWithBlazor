# Development Commands & Scripts Reference

## 🚀 Quick Start Commands

### Development Environment Startup
```powershell
# Automated startup (recommended)
powershell -ExecutionPolicy Bypass -File .\start-dev.ps1

# Manual startup (if script fails)
# Terminal 1 - API
cd src/LoanSuperMarket.Api
dotnet run

# Terminal 2 - Blazor Frontend  
cd src/LoanSuperMarket.Blazor
dotnet run

# Terminal 3 - TailwindCSS Watcher
cd src/LoanSuperMarket.Blazor
npm run watch
```

### Application URLs
- **API**: https://localhost:7001
- **Blazor Frontend**: https://localhost:5036
- **Swagger Documentation**: https://localhost:7001/swagger

## 🛠️ Development Scripts

### PowerShell Execution Policy Setup
```powershell
# One-time setup (run as Administrator)
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned

# Verify policy
Get-ExecutionPolicy -Scope CurrentUser
```

### Database Management
```powershell
# Create new migration
cd src/LoanSuperMarket.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../LoanSuperMarket.Api

# Update database
dotnet ef database update --startup-project ../LoanSuperMarket.Api

# Drop database (careful!)
dotnet ef database drop --startup-project ../LoanSuperMarket.Api

# Generate SQL script
dotnet ef migrations script --startup-project ../LoanSuperMarket.Api
```

### Build & Test Commands
```powershell
# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## 🎨 Frontend Development

### TailwindCSS Commands
```powershell
# Install dependencies (first time)
cd src/LoanSuperMarket.Blazor
npm install

# Watch for changes (development)
npm run watch

# Build for production
npm run build

# Update TailwindCSS
npm update tailwindcss

# Install additional packages
npm install @tailwindcss/forms
npm install @tailwindcss/typography
```

### Blazor Development Commands
```powershell
# Hot reload development
cd src/LoanSuperMarket.Blazor
dotnet watch run

# Build optimized for production
dotnet publish -c Release

# Analyze bundle size
dotnet publish -c Release --verbosity detailed
```

## 🧪 Testing & Quality

### Unit Testing
```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test src/LoanSuperMarket.Tests.Unit

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests in parallel
dotnet test --parallel

# Generate test report
dotnet test --logger trx --results-directory ./TestResults
```

### Code Quality Tools
```powershell
# Install code analysis tools
dotnet tool install --global dotnet-reportgenerator-globaltool
dotnet tool install --global dotnet-stryker

# Run code coverage
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

# Run mutation testing
dotnet stryker
```

## 📦 Package Management

### NuGet Package Commands
```powershell
# Add package to specific project
dotnet add src/LoanSuperMarket.Api package PackageName

# Remove package
dotnet remove src/LoanSuperMarket.Api package PackageName

# Update all packages
dotnet restore --force

# List outdated packages
dotnet list package --outdated

# Update specific package
dotnet add package PackageName --version 1.2.3
```

### Project References
```powershell
# Add project reference
dotnet add src/LoanSuperMarket.Api reference src/LoanSuperMarket.Application

# Remove project reference
dotnet remove src/LoanSuperMarket.Api reference src/LoanSuperMarket.Application

# List project references
dotnet list src/LoanSuperMarket.Api reference
```

## 🔧 Development Tools

### Entity Framework Tools
```powershell
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Update EF Core tools
dotnet tool update --global dotnet-ef

# Verify EF Core tools
dotnet ef --version

# Scaffold DbContext from existing database
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Models
```

### Code Generation
```powershell
# Generate API controller
dotnet aspnet-codegenerator controller -name ProductsController -api -m Product -dc ApplicationDbContext

# Generate Razor pages
dotnet aspnet-codegenerator razorpage -m Product -dc ApplicationDbContext -udl -outDir Pages/Products
```

## 🐛 Debugging & Diagnostics

### Logging & Diagnostics
```powershell
# Enable detailed logging
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:Logging__LogLevel__Default="Debug"

# Run with specific logging level
dotnet run --environment Development --verbosity detailed

# Capture HTTP traffic
dotnet run --urls "https://localhost:7001" --capture-startup-errors
```

### Performance Profiling
```powershell
# Install diagnostic tools
dotnet tool install --global dotnet-counters
dotnet tool install --global dotnet-dump
dotnet tool install --global dotnet-trace

# Monitor performance counters
dotnet-counters monitor --process-id <PID>

# Capture memory dump
dotnet-dump collect --process-id <PID>

# Trace application performance
dotnet-trace collect --process-id <PID>
```

## 🚀 Deployment Commands

### Docker Commands
```powershell
# Build Docker image
docker build -t loansupermarket-api -f src/LoanSuperMarket.Api/Dockerfile .
docker build -t loansupermarket-blazor -f src/LoanSuperMarket.Blazor/Dockerfile .

# Run containers
docker run -p 7001:80 loansupermarket-api
docker run -p 5036:80 loansupermarket-blazor

# Docker Compose
docker-compose up -d
docker-compose down
docker-compose logs -f
```

### Azure Deployment
```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI

# Login to Azure
az login

# Create resource group
az group create --name LoanSuperMarketRG --location "East US"

# Create App Service plan
az appservice plan create --name LoanSuperMarketPlan --resource-group LoanSuperMarketRG --sku B1

# Create web apps
az webapp create --name loansupermarket-api --resource-group LoanSuperMarketRG --plan LoanSuperMarketPlan
az webapp create --name loansupermarket-blazor --resource-group LoanSuperMarketRG --plan LoanSuperMarketPlan

# Deploy applications
az webapp deployment source config-zip --name loansupermarket-api --resource-group LoanSuperMarketRG --src api.zip
az webapp deployment source config-zip --name loansupermarket-blazor --resource-group LoanSuperMarketRG --src blazor.zip
```

## 📊 Monitoring & Maintenance

### Health Checks
```powershell
# Check application health
curl https://localhost:7001/health
curl https://localhost:5036/health

# Database connectivity test
dotnet run --project src/LoanSuperMarket.Api -- --test-db-connection

# Performance benchmarks
dotnet run --project src/LoanSuperMarket.Benchmarks -c Release
```

### Log Analysis
```powershell
# View application logs
Get-Content "logs/app.log" -Tail 50 -Wait

# Search for errors
Select-String -Path "logs/*.log" -Pattern "ERROR|EXCEPTION"

# Analyze performance logs
Select-String -Path "logs/*.log" -Pattern "took \d+ms" | Sort-Object
```

## 🔄 Git Workflow Commands

### Development Workflow
```powershell
# Create feature branch
git checkout -b feature/loan-product-approval

# Stage and commit changes
git add .
git commit -m "feat: implement loan product approval workflow"

# Push feature branch
git push -u origin feature/loan-product-approval

# Create pull request (using GitHub CLI)
gh pr create --title "Implement loan product approval workflow" --body "Adds approval workflow with confirmation dialogs and notifications"

# Merge and cleanup
git checkout main
git pull origin main
git branch -d feature/loan-product-approval
```

### Release Management
```powershell
# Create release branch
git checkout -b release/v1.2.0

# Tag release
git tag -a v1.2.0 -m "Release version 1.2.0"
git push origin v1.2.0

# Generate changelog
git log --oneline --since="2024-01-01" --until="2024-02-01" > CHANGELOG.md
```

## 🛡️ Security & Compliance

### Security Scanning
```powershell
# Install security tools
dotnet tool install --global security-scan

# Scan for vulnerabilities
dotnet list package --vulnerable
dotnet list package --deprecated

# Update vulnerable packages
dotnet add package PackageName --version LatestSecureVersion
```

### Code Analysis
```powershell
# Run static analysis
dotnet build --verbosity normal /p:RunAnalyzersDuringBuild=true

# Generate security report
dotnet security-scan --project src/LoanSuperMarket.Api/LoanSuperMarket.Api.csproj
```

This comprehensive command reference ensures you can efficiently develop, test, deploy, and maintain the enterprise-grade Loan Investment Supermarket platform.