param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectName
)

$ErrorActionPreference = "Stop"

$WorkspaceRoot = Split-Path $PSScriptRoot -Parent
$ProjectDir = Join-Path $WorkspaceRoot "projects\$ProjectName"

if (-not (Test-Path $ProjectDir)) {
    Write-Error "Project directory not found: $ProjectDir"
    exit 1
}

# Find all .csproj / .sln in the project
$ProjectFiles = Get-ChildItem -Path $ProjectDir -Filter "*.csproj" -Recurse | Where-Object { $_.FullName -notmatch "tests" }

if ($ProjectFiles.Count -eq 0) {
    Write-Host "No C# projects (.csproj) found in $ProjectName." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($ProjectFiles.Count) C# Project(s). Starting Build..." -ForegroundColor Cyan

$SuccessCount = 0
foreach ($Proj in $ProjectFiles) {
    Write-Host "`nBuilding C# Program: $($Proj.Name)" -ForegroundColor Cyan
    
    # Restore and Build
    dotnet build "$($Proj.FullName)" -c Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully compiled $($Proj.Name)" -ForegroundColor Green
        $SuccessCount++
    }
    else {
        Write-Host "Build Failed for $($Proj.Name) with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`nBuild Summary: $SuccessCount C# project(s) compiled successfully." -ForegroundColor Green
exit 0
