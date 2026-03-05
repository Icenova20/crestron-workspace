param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName
)

$templateDir = Join-Path "html" "_template"
$targetDir = Join-Path "html" $ProjectName

if (-not (Test-Path $templateDir)) {
    Write-Error "Template directory '$templateDir' not found."
    return
}

if (Test-Path $targetDir) {
    Write-Error "Project '$ProjectName' already exists."
    return
}

Write-Host "Creating project '$ProjectName' from template..."
Copy-Item -Path $templateDir -Destination $targetDir -Recurse

Write-Host "New project created at $targetDir"
