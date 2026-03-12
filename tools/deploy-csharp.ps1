param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectName,
    
    [Parameter(Mandatory = $false)]
    [string]$ProcessorIP = "172.22.0.1",
    
    [Parameter(Mandatory = $false)]
    [string]$Username = "admin",
    
    [Parameter(Mandatory = $false)]
    [string]$Password,
    
    [Parameter(Mandatory = $false)]
    [string]$ProgramSlot = "1",
    
    [Parameter(Mandatory = $false)]
    [switch]$DebugMode
)

$ErrorActionPreference = "Stop"
$WorkspaceRoot = Split-Path $PSScriptRoot -Parent
$ProjectDir = Join-Path $WorkspaceRoot "projects\$ProjectName"

if (-not (Test-Path $ProjectDir)) {
    Write-Error "Project directory not found: $ProjectDir"
    exit 1
}

# 1. Locate the newest .cpz File
$CpzFiles = Get-ChildItem -Path $ProjectDir -Filter "*.cpz" -Recurse | Where-Object { 
    $_.FullName -match "bin.*Release" -and $_.FullName -notmatch "tests" 
} | Sort-Object LastWriteTime -Descending

if ($CpzFiles.Count -eq 0) {
    Write-Error "No compiled .cpz file found in Release directory for project $ProjectName."
    exit 1
}

$TargetCpz = $CpzFiles[0]
Write-Host "Found CPZ: $($TargetCpz.FullName)" -ForegroundColor Cyan
Write-Host "Deploying to slot $ProgramSlot on $ProcessorIP..." -ForegroundColor Cyan

# 2. Automation Prep (Require Posh-SSH if password is provided)
if ([string]::IsNullOrWhiteSpace($Password)) {
    # Fallback to interactive SCP
    $RemoteDir = "/NVRAM"
    Write-Host "No password provided. Transferring via interactive SCP..." -ForegroundColor Yellow
    scp "$($TargetCpz.FullName)" "$Username@${ProcessorIP}:$RemoteDir/"
    
    if ($DebugMode) {
        Write-Host "Configuring program $ProgramSlot for Remote Debugging (Port 50000)..." -ForegroundColor Magenta
        ssh "$Username@$ProcessorIP" "DEBUGPROGRAM -P$ProgramSlot -Port:50000 -IP:0.0.0.0 -S"
    }
    else {
        # Clear debug flag internally just in case it was previously debugging
        ssh "$Username@$ProcessorIP" "DEBUGPROGRAM -P$ProgramSlot -C" | Out-Null
    }

    Write-Host "Restarting program $ProgramSlot via SSH PROGLOAD..." -ForegroundColor Yellow
    ssh "$Username@$ProcessorIP" "PROGLOAD -P$ProgramSlot -File:\NVRAM\$($TargetCpz.Name)"
}
else {
    # Use Posh-SSH for automated pipeline
    if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
        Write-Host "Posh-SSH module not found. Installing for current user..." -ForegroundColor Cyan
        Install-Module -Name Posh-SSH -Force -Scope CurrentUser -AllowClobber
    }
    Import-Module Posh-SSH

    $secPass = ConvertTo-SecureString $Password -AsPlainText -Force
    $cred = New-Object System.Management.Automation.PSCredential ($Username, $secPass)
    
    # Calculate Crestron SFTP relative path (no leading slash)
    $slotDir = "program$($ProgramSlot.PadLeft(2, '0'))"
    
    Write-Host "Establishing SSH Session..." -ForegroundColor Cyan
    try {
        $session = New-SSHSession -ComputerName $ProcessorIP -Credential $cred -AcceptKey -ErrorAction Stop
    }
    catch {
        Write-Host "New-SSHSession Failed: $($_.Exception.Message)" -ForegroundColor Red
        throw $_
    }
    
    Write-Host "Transferring via Posh-SSH SFTP to $slotDir/..." -ForegroundColor Yellow
    try {
        $sftpSession = New-SFTPSession -ComputerName $ProcessorIP -Credential $cred -AcceptKey -ErrorAction Stop
        Set-SFTPItem -SessionId $sftpSession.SessionId -Path "$($TargetCpz.FullName)" -Destination "$slotDir/" -Force
        Remove-SFTPSession -SessionId $sftpSession.SessionId | Out-Null
    }
    catch {
        Write-Host "SFTP Transfer Failed: $($_.Exception.Message)" -ForegroundColor Red
        throw $_
    }
    
    if ($DebugMode) {
        Write-Host "Configuring program $ProgramSlot for Remote Debugging (Port 50000)..." -ForegroundColor Magenta
        Invoke-SSHCommand -SessionId $session.SessionId -Command "DEBUGPROGRAM -P$ProgramSlot -Port:50000 -IP:0.0.0.0 -S" | Out-Null
    }
    else {
        Invoke-SSHCommand -SessionId $session.SessionId -Command "DEBUGPROGRAM -P$ProgramSlot -C" | Out-Null
    }
    
    Write-Host "Restarting program $ProgramSlot via PROGLOAD..." -ForegroundColor Yellow
    # Note: Using backslashes for the CP4N console path
    $remoteCpzPath = "\ROMDisk\User\prog$ProgramSlot\$($TargetCpz.Name)"
    $result = Invoke-SSHCommand -SessionId $session.SessionId -Command "PROGLOAD -P$ProgramSlot -File:$remoteCpzPath"
    
    if ($result.Error) { Write-Host $result.Error -ForegroundColor Red }
    if ($result.Output) { Write-Host $result.Output -ForegroundColor DarkGray }
    
    Remove-SSHSession -SessionId $session.SessionId | Out-Null
}

Write-Host "Deployment completed." -ForegroundColor Green
