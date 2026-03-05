# generate-sig.ps1
# Parses JoinMap.cs and generates a .sig file for the Crestron Toolbox Signal Debugger

$joinMapPath = "d:\Antigravity\lockton-test\src\JoinMap.cs"
$outputPath = "d:\Antigravity\lockton-test\src\bin\Any CPU\Release\net47\LocktonTest.sig"

if (-not (Test-Path $joinMapPath)) {
    Write-Error "JoinMap.cs not found at $joinMapPath"
    return
}

$content = Get-Content $joinMapPath
$sigLines = @()

# Simple regex to find "public const uint Name = Value;"
# We look for digital/analog based on comments or context in JoinMap.cs
# In our JoinMap: 
# - Level = Analog
# - Mute/Power/Select/Sonos/AutoOn/AutoOff = Digital

foreach ($line in $content) {
    if ($line -match 'public const uint (\w+)\s*=\s*(\d+);') {
        $name = $Matches[1]
        $join = $Matches[2]
        
        $prefix = "D" # Default to Digital
        
        if ($name.ToLower().Contains("level") -or $name.ToLower().Contains("analog")) {
            $prefix = "A"
        }
        
        $sigLines += "$prefix$join`:$name"
    }
}

$sigLines | Out-File -FilePath $outputPath -Encoding ascii
Write-Host "Generated .sig file at $outputPath" -ForegroundColor Green
