# Create a global builds directory if it doesn't exist
$buildsDir = Join-Path (Get-Location) "builds"
if (-not (Test-Path $buildsDir)) {
    New-Item -ItemType Directory -Path $buildsDir
}

# Compiles all projects in the html/ folder (excluding _template)
Get-ChildItem -Path "html" -Directory | ForEach-Object {
    $foldername = $_.Name
    if ($foldername -eq "_template") { return }

    # Create a safe slug for the project name (no spaces/special chars)
    # Truncate to 50 chars to avoid Crestron's 64-character limit
    $slug = $foldername.ToLower() -replace '[^a-z0-9]', '_' -replace '__+', '_'
    if ($slug.Length -gt 50) { $slug = $slug.Substring(0, 50).TrimEnd('_') }
    
    Write-Host "------------------------------------------------"
    Write-Host "Building project: $foldername ($slug)..."
    Write-Host "------------------------------------------------"

    # Save current location and change to project dir
    $origLoc = Get-Location
    Set-Location $_.FullName
    
    try {
        # Clean up any old local artifacts
        if (Test-Path "temp") { Remove-Item -Recurse -Force "temp" }
        Get-ChildItem -Filter "*.ch5z" | Remove-Item -Force -ErrorAction SilentlyContinue
        
        # Try to use local node_modules version first for reliability
        $localCli = "node_modules/@crestron/ch5-utilities-cli/build/index.js"
        if (Test-Path $localCli) {
            node $localCli archive -p "$slug" -d . -o "../../builds"
        }
        else {
            # Fallback to npx if node_modules is missing
            npx -y --package=@crestron/ch5-utilities-cli ch5-cli archive -p "$slug" -d . -o "../../builds"
        }
    }
    finally {
        # Final cleanup of the tool's temp folder
        if (Test-Path "temp") { Remove-Item -Recurse -Force "temp" }
        Set-Location $origLoc
    }
}

Write-Host "------------------------------------------------"
Write-Host "Build complete. Archives are in the /builds folder."
Write-Host "------------------------------------------------"
