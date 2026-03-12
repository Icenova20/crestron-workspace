$msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
$projectPath = "d:\Antigravity\lockton-test\src\LocktonTest.csproj"

if (-not (Test-Path $msbuildPath)) {
    Write-Error "MSBuild not found at $msbuildPath. Please ensure Visual Studio 2022 is installed."
    return
}

Write-Host "Building C# Program: $projectPath" -ForegroundColor Cyan
Set-Location "d:\Antigravity\lockton-test\src"
& $msbuildPath "LocktonTest.csproj" /t:Restore
& $msbuildPath "LocktonTest.csproj" /t:Build /p:Configuration=Release /p:Platform="Any CPU"

if ($LASTEXITCODE -eq 0) {
    Set-Location "d:\Antigravity\lockton-test"
    powershell -ExecutionPolicy Bypass -File .\generate-sig.ps1
    Write-Host "`nBuild Successful!" -ForegroundColor Green
}
else {
    Write-Host "`nBuild Failed with exit code $LASTEXITCODE" -ForegroundColor Red
}
