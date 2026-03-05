function Check-Command($cmd) {
    if (Get-Command $cmd -ErrorAction SilentlyContinue) {
        Write-Host "[OK] $cmd is installed." -ForegroundColor Green
    }
    else {
        # Check standard installation paths if not in PATH
        $stdPath = ""
        if ($cmd -eq "node") { $stdPath = "$env:ProgramFiles\nodejs\node.exe" }
        if ($cmd -eq "npm") { $stdPath = "$env:ProgramFiles\nodejs\npm.cmd" }
        if ($cmd -eq "npx") { $stdPath = "$env:ProgramFiles\nodejs\npx.cmd" }
        if ($cmd -eq "msbuild") { $stdPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" }
        
        if ($stdPath -ne "" -and (Test-Path $stdPath)) {
            Write-Host "[FOUND] $cmd is installed at $stdPath, but not in your PATH." -ForegroundColor Yellow
            Write-Host "  -> Try restarting your IDE/Terminal to refresh the environment."
        }
        else {
            Write-Host "[MISSING] $cmd is not found." -ForegroundColor Red
            if ($cmd -eq "npx") { Write-Host "  -> Install Node.js from https://nodejs.org/" }
            if ($cmd -eq "python") { Write-Host "  -> Install Python from https://www.python.org/" }
            if ($cmd -eq "msbuild") { 
                Write-Host "  -> MSBuild is the build engine for .NET. You get it by installing Visual Studio 2022."
                Write-Host "  -> Download: https://visualstudio.microsoft.com/vs/community/"
                Write-Host "  -> Workload required: '.NET Desktop Development'."
            }
        }
    }
}

Write-Host "--- Checking Development Environment ---"
Check-Command "node"
Check-Command "npm"
Check-Command "npx"
Check-Command "python"
Check-Command "git"
Check-Command "msbuild"

Write-Host "--- Project Paths ---"
Write-Host "CH5 Workspace: d:\Antigravity\ch5-workspace\"
Write-Host "Test Program:  d:\Antigravity\lockton-test\"

Write-Host "`nReady for Windows 11 Development."
