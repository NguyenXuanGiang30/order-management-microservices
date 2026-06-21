param(
  [switch]$SkipMigrations
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
$Logs = Join-Path $Root '.dev-logs'

New-Item -ItemType Directory -Force -Path $Logs | Out-Null

function Wait-Docker {
  docker info *> $null
  if ($LASTEXITCODE -eq 0) {
    return
  }

  $dockerDesktop = 'C:\Program Files\Docker\Docker\Docker Desktop.exe'
  if (Test-Path -LiteralPath $dockerDesktop) {
    Start-Process -FilePath $dockerDesktop -WindowStyle Hidden
  }

  for ($i = 1; $i -le 45; $i++) {
    docker info *> $null
    if ($LASTEXITCODE -eq 0) {
      return
    }

    Start-Sleep -Seconds 2
  }

  throw 'Docker daemon is not ready. Start Docker Desktop and rerun this script.'
}

function Wait-Port([int]$Port, [int]$TimeoutSeconds = 60) {
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($connection) {
      return
    }

    Start-Sleep -Seconds 1
  }

  throw "Port $Port did not start listening within $TimeoutSeconds seconds."
}

function Start-DotNetService(
  [string]$Name,
  [string]$Project,
  [int]$Port,
  [string]$ConnectionString = $null
) {
  $existing = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
  if ($existing) {
    Write-Host "$Name already listening on port $Port."
    return
  }

  $projectPath = Join-Path $Root $Project
  $workingDirectory = Split-Path -Parent $projectPath
  $stdout = Join-Path $Logs "$Name.out.log"
  $stderr = Join-Path $Logs "$Name.err.log"

  if ($ConnectionString) {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
  }
  $env:ASPNETCORE_ENVIRONMENT = 'Development'
  $process = Start-Process `
    -FilePath 'dotnet' `
    -ArgumentList @('run', '--no-build', '--project', $projectPath, '--urls', "http://localhost:$Port") `
    -WorkingDirectory $workingDirectory `
    -RedirectStandardOutput $stdout `
    -RedirectStandardError $stderr `
    -WindowStyle Hidden `
    -PassThru

  Write-Host "Started $Name PID $($process.Id) on port $Port."
  Wait-Port -Port $Port
}

function Test-Health([string]$Name, [string]$Url) {
  try {
    $response = Invoke-WebRequest -UseBasicParsing $Url -TimeoutSec 15
    Write-Host "$Name health: $($response.StatusCode) $($response.Content)"
  }
  catch {
    Write-Warning "$Name health failed: $($_.Exception.Message)"
  }
}

Set-Location $Root

Write-Host 'Starting databases and message broker containers...'
Wait-Docker
docker compose up -d sqlserver rabbitmq
Write-Host 'Waiting for SQL Server (1433) and RabbitMQ (5672) to be ready...'
Wait-Port -Port 1433
Wait-Port -Port 5672

if (-not $SkipMigrations) {
  Write-Host 'Applying database migrations...'
  
  $hasDotNetEf = $null -ne (Get-Command dotnet-ef -ErrorAction SilentlyContinue)
  if ($hasDotNetEf) {
    $env:ConnectionStrings__DefaultConnection = 'Server=127.0.0.1;Database=UserReportDB;User Id=sa;Password=SuperStrong@Password123;TrustServerCertificate=True;'
    dotnet ef database update `
      --project src\UserReportService\UserReportService.Infrastructure\UserReportService.Infrastructure.csproj `
      --startup-project src\UserReportService\UserReportService.API\UserReportService.API.csproj

    $env:ConnectionStrings__DefaultConnection = 'Server=127.0.0.1;Database=ProductInventoryDB;User Id=sa;Password=SuperStrong@Password123;TrustServerCertificate=True;'
    dotnet ef database update `
      --project src\ProductInventoryService\ProductInventoryService.Infrastructure\ProductInventoryService.Infrastructure.csproj `
      --startup-project src\ProductInventoryService\ProductInventoryService.API\ProductInventoryService.API.csproj

    $env:ConnectionStrings__DefaultConnection = 'Server=127.0.0.1;Database=OrderSalesDB;User Id=sa;Password=SuperStrong@Password123;TrustServerCertificate=True;'
    dotnet ef database update `
      --project src\OrderSalesService\OrderSalesService.Infrastructure\OrderSalesService.Infrastructure.csproj `
      --startup-project src\OrderSalesService\OrderSalesService.API\OrderSalesService.API.csproj
  } else {
    Write-Warning 'dotnet-ef tool is not installed. Database migrations will be applied automatically on application startup.'
  }
}

Write-Host 'Building backend services...'
dotnet build src\ApiGateway\ApiGateway\ApiGateway.csproj
dotnet build src\UserReportService\UserReportService.API\UserReportService.API.csproj
dotnet build src\ProductInventoryService\ProductInventoryService.API\ProductInventoryService.API.csproj
dotnet build src\OrderSalesService\OrderSalesService.API\OrderSalesService.API.csproj

Start-DotNetService `
  -Name 'api-gateway' `
  -Project 'src\ApiGateway\ApiGateway\ApiGateway.csproj' `
  -Port 5000

Start-DotNetService `
  -Name 'user-report-service' `
  -Project 'src\UserReportService\UserReportService.API\UserReportService.API.csproj' `
  -Port 5160 `
  -ConnectionString 'Server=127.0.0.1;Database=UserReportDB;User Id=sa;Password=SuperStrong@Password123;TrustServerCertificate=True;'

Start-DotNetService `
  -Name 'product-inventory-service' `
  -Project 'src\ProductInventoryService\ProductInventoryService.API\ProductInventoryService.API.csproj' `
  -Port 5178 `
  -ConnectionString 'Server=127.0.0.1;Database=ProductInventoryDB;User Id=sa;Password=SuperStrong@Password123;TrustServerCertificate=True;'

Start-DotNetService `
  -Name 'order-sales-service' `
  -Project 'src\OrderSalesService\OrderSalesService.API\OrderSalesService.API.csproj' `
  -Port 5245 `
  -ConnectionString 'Server=127.0.0.1;Database=OrderSalesDB;User Id=sa;Password=SuperStrong@Password123;TrustServerCertificate=True;'

Start-Sleep -Seconds 3

Test-Health -Name 'Gateway' -Url 'http://localhost:5000/health'
Test-Health -Name 'UserReportService' -Url 'http://localhost:5160/health'
Test-Health -Name 'ProductInventoryService' -Url 'http://localhost:5178/health'
Test-Health -Name 'OrderSalesService' -Url 'http://localhost:5245/health'

Write-Host 'RetailOps dev backend is ready.'
Write-Host 'RabbitMQ UI: http://localhost:15672 (guest / guest)'
