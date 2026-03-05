param(
    [string]$TargetUsh,
    [string]$ModularSimplDir
)

$ErrorActionPreference = "Stop"
$simplDir = $ModularSimplDir
if (-not $simplDir) { $simplDir = Join-Path (Split-Path $PSScriptRoot -Parent) "modules\simpl" }

if ($TargetUsh) {
    if (-not (Test-Path $TargetUsh)) { throw "Target USH not found: $TargetUsh" }
    $ushFiles = @(Get-Item $TargetUsh)
} else {
    $ushFiles = Get-ChildItem -Path $simplDir -Filter "*.ush"
}

function Parse-Ush {
    param([string]$path)
    $lines = Get-Content $path
    $dict = @{}
    foreach ($line in $lines) {
        $line = $line.Trim()
        if ($line.Contains("=")) {
            $split = $line.Split("=", 2)
            $dict[$split[0]] = $split[1]
        }
    }
    
    if (-not $dict.ContainsKey("Name")) { throw "USH file is missing 'Name' key: $path" }
    
    $module = @{
        Name = $dict["Name"] -replace " \(Macro\)", ""
        SmplCName = $dict["SmplCName"]
        MaxI = [int]($dict["MaxVariableInputs"] ?? 0)
        MaxI2 = [int]($dict["MaxVariableInputsList2"] ?? 0)
        MaxO = [int]($dict["MaxVariableOutputs"] ?? 0)
        MaxO2 = [int]($dict["MaxVariableOutputsList2"] ?? 0)
        NumFixedP = [int]($dict["NumFixedParams"] ?? 0)
        MaxP = [int]($dict["MaxVariableParams"] ?? 0)
        TotalP = [int]($dict["NumFixedParams"] ?? 0) + [int]($dict["MaxVariableParams"] ?? 0)
        Inputs = @()
        Outputs = @()
        Params = @()
        Comments = @()
    }
    
    # Capture Comments/Help text
    for ($i = 1; $i -le 100; $i++) {
        if ($dict.ContainsKey("Cmn$i")) {
            $module.Comments += $dict["Cmn$i"]
        }
    }
    
    for ($i = 1; $i -le $module.MaxI; $i++) {
        $cue = $dict["InputCue$i"]
        if ($cue -and $cue -notmatch "_SKIP_") {
            if ($cue -match "\[#\]$") {
                $base = $cue -replace "\[#\]$", ""
                $count = $module.MaxI - $i + 1
                for ($j = 1; $j -le $count; $j++) {
                    $module.Inputs += [PSCustomObject]@{ InnerId = ($i + $j - 1); Name = "$base[$j]"; List = 1; Tp = 2; SigType = "Digital" }
                }
                break # Arrays are always at the end in S+ variable lists
            }
            else {
                $module.Inputs += [PSCustomObject]@{ InnerId = $i; Name = $cue; List = 1; Tp = 2; SigType = "Digital" }
            }
        }
    }
    for ($i = 1; $i -le $module.MaxI2; $i++) {
        $cue = $dict["InputList2Cue$i"]
        $type = $dict["InputList2SigType$i"]
        if ($cue -and $cue -notmatch "_SKIP_") {
            if ($cue -match "\[#\]$") {
                $base = $cue -replace "\[#\]$", ""
                $count = $module.MaxI2 - $i + 1
                for ($j = 1; $j -le $count; $j++) {
                    $tp = if ($type -match "Analog") { 4 } else { 6 }
                    $cleanType = if ($type -match "Analog") { "Analog" } else { "Serial" }
                    $module.Inputs += [PSCustomObject]@{ InnerId = ($i + $j - 1); Name = "$base[$j]"; List = 2; Tp = $tp; SigType = $cleanType }
                }
                break
            }
            else {
                $tp = if ($type -match "Analog") { 4 } else { 6 }
                $cleanType = if ($type -match "Analog") { "Analog" } else { "Serial" }
                $module.Inputs += [PSCustomObject]@{ InnerId = $i; Name = $cue; List = 2; Tp = $tp; SigType = $cleanType }
            }
        }
    }
    
    for ($i = 1; $i -le $module.MaxO; $i++) {
        $cue = $dict["OutputCue$i"]
        if ($cue -and $cue -notmatch "_SKIP_") {
            if ($cue -match "\[#\]$") {
                $base = $cue -replace "\[#\]$", ""
                $count = $module.MaxO - $i + 1
                for ($j = 1; $j -le $count; $j++) {
                    $module.Outputs += [PSCustomObject]@{ InnerId = ($i + $j - 1); Name = "$base[$j]"; List = 1; Tp = 3; SigType = "Digital" }
                }
                break
            }
            else {
                $module.Outputs += [PSCustomObject]@{ InnerId = $i; Name = $cue; List = 1; Tp = 3; SigType = "Digital" }
            }
        }
    }
    for ($i = 1; $i -le $module.MaxO2; $i++) {
        $cue = $dict["OutputList2Cue$i"]
        $type = $dict["OutputList2SigType$i"]
        if ($cue -and $cue -notmatch "_SKIP_") {
            if ($cue -match "\[#\]$") {
                $base = $cue -replace "\[#\]$", ""
                $count = $module.MaxO2 - $i + 1
                for ($j = 1; $j -le $count; $j++) {
                    $tp = if ($type -match "Analog") { 5 } else { 6 }
                    $cleanType = if ($type -match "Analog") { "Analog" } else { "Serial" }
                    $module.Outputs += [PSCustomObject]@{ InnerId = ($i + $j - 1); Name = "$base[$j]"; List = 2; Tp = $tp; SigType = $cleanType }
                }
                break
            }
            else {
                $tp = if ($type -match "Analog") { 5 } else { 6 }
                $cleanType = if ($type -match "Analog") { "Analog" } else { "Serial" }
                $module.Outputs += [PSCustomObject]@{ InnerId = $i; Name = $cue; List = 2; Tp = $tp; SigType = $cleanType }
            }
        }
    }
    
    for ($i = 1; $i -le $module.TotalP; $i++) {
        $cue = $dict["ParamCue$i"]
        if ($cue -and $cue -notmatch "_SKIP_" -and $cue -notmatch "\[Reference Name\]" -and $cue -notmatch "\[~UNUSED~\]") {
            $module.Params += [PSCustomObject]@{ InnerId = $i; Name = $cue; Tp = 1 }
        }
    }
    
    return $module
}

function Get-BaseName {
    param([string]$name)
    if ($name -match "^([^\[]+)\[") { return $matches[1] }
    return $name
}

function Generate-Umc {
    param($module)
    
    $umcName = $module.SmplCName.Replace(".usp", ".umc")
    $umcBaseName = $umcName -replace "\.umc$", ""
    $umcPath = Join-Path $simplDir $umcName
    
    $category = "Lockton Modules"
    if ($umcBaseName -match "^Cisco_") { $category = "Video Conferencing" }
    elseif ($umcBaseName -match "^Planar_") { $category = "Displays" }
    elseif ($umcBaseName -match "^Shure_") { $category = "Microphones" }
    elseif ($umcBaseName -match "^Sonos_") { $category = "Audio Players" }
    elseif ($umcBaseName -match "^Nvx_") { $category = "Video Routing" }
    
    Write-Host "Generating: $umcName [$category]" -ForegroundColor Cyan
    
    $macroInputs = @()
    $prevBase = ""
    
    foreach ($pin in $module.Inputs) {
        $base = Get-BaseName $pin.Name
        if ($prevBase -ne "" -and $base -ne $prevBase) {
            $macroInputs += [PSCustomObject]@{ Name = "[~UNUSED~]"; IsGap = $true; IsGroup = $false }
        }
        $macroInputs += [PSCustomObject]@{ Name = $pin.Name; InnerId = $pin.InnerId; List = $pin.List; Tp = $pin.Tp; SigType = $pin.SigType; IsGap = $false; IsGroup = $false }
        $prevBase = $base
    }
    
    $macroOutputs = @()
    $prevBase = ""
    foreach ($pin in $module.Outputs) {
        $base = Get-BaseName $pin.Name
        if ($prevBase -ne "" -and $base -ne $prevBase) {
            $macroOutputs += [PSCustomObject]@{ Name = "[~UNUSED~]"; IsGap = $true; IsGroup = $false }
        }
        $macroOutputs += [PSCustomObject]@{ Name = $pin.Name; InnerId = $pin.InnerId; List = $pin.List; Tp = $pin.Tp; SigType = $pin.SigType; IsGap = $false; IsGroup = $false }
        $prevBase = $base
    }
    
    $macroParams = @()
    foreach ($pin in $module.Params) {
        $macroParams += [PSCustomObject]@{ Name = $pin.Name; InnerId = $pin.InnerId; Tp = $pin.Tp }
    }

    $sgId = 100
    $dpId = 500
    $signalBlocks = ""
    $dpBlocks = ""
    $unusedSgId = 9999
    
    for ($i=0; $i -lt $macroInputs.Count; $i++) {
        $pin = $macroInputs[$i]
        $pin | Add-Member -NotePropertyName MacroId -NotePropertyValue ($i+1)
        
        $sgTpLine = if ($pin.IsGap) { "`nSgTp=31" } elseif ($pin.SigType -eq "Analog") { "`nSgTp=2" } elseif ($pin.SigType -eq "Serial") { "`nSgTp=4" } else { "" }
        
        if ($pin.IsGap) { 
            $pin | Add-Member -NotePropertyName SgId -NotePropertyValue $unusedSgId
            $signalBlocks += "[`nObjTp=Sg`nH=$unusedSgId`nNm=[~UNUSED~]$sgTpLine`n]`n"
            continue 
        }
        
        $pin | Add-Member -NotePropertyName SgId -NotePropertyValue $sgId
        $pin | Add-Member -NotePropertyName DpId -NotePropertyValue $dpId
        $signalBlocks += "[`nObjTp=Sg`nH=$sgId`nNm=$($pin.Name)$sgTpLine`n]`n"
        $dpProp = if ($pin.Tp -eq 2) { "Trg=2" } elseif ($pin.Tp -eq 4 -or $pin.Tp -eq 5) { "NF=63`nDNF=1" } else { "" }
        $dpPropStr = if ($dpProp) { "`n$dpProp" } else { "" }
        $dpBlocks += "[`nObjTp=Dp`nH=$dpId`nTp=$($pin.Tp)`nSD=$($pin.Name)$dpPropStr`n]`n"
        $sgId++; $dpId++
    }
    
    for ($i=0; $i -lt $macroOutputs.Count; $i++) {
        $pin = $macroOutputs[$i]
        $pin | Add-Member -NotePropertyName MacroId -NotePropertyValue ($i+1)
        
        $sgTpLine = if ($pin.IsGap) { "`nSgTp=31" } elseif ($pin.SigType -eq "Analog") { "`nSgTp=2" } elseif ($pin.SigType -eq "Serial") { "`nSgTp=4" } else { "" }
        
        if ($pin.IsGap) { 
            $pin | Add-Member -NotePropertyName SgId -NotePropertyValue $unusedSgId
            $signalBlocks += "[`nObjTp=Sg`nH=$unusedSgId`nNm=[~UNUSED~]$sgTpLine`n]`n"
            continue 
        }
        
        $pin | Add-Member -NotePropertyName SgId -NotePropertyValue $sgId
        $pin | Add-Member -NotePropertyName DpId -NotePropertyValue $dpId
        $signalBlocks += "[`nObjTp=Sg`nH=$sgId`nNm=$($pin.Name)$sgTpLine`n]`n"
        $dpBlocks += "[`nObjTp=Dp`nH=$dpId`nTp=$($pin.Tp)`nSD=$($pin.Name)`n]`n"
        $sgId++; $dpId++
    }
    
    for ($i=0; $i -lt $macroParams.Count; $i++) {
        $pin = $macroParams[$i]
        $pin | Add-Member -NotePropertyName MacroId -NotePropertyValue ($i+1)
        
        $pin | Add-Member -NotePropertyName SgId -NotePropertyValue $sgId
        $pin | Add-Member -NotePropertyName DpId -NotePropertyValue $dpId
        $signalBlocks += "[`nObjTp=Sg`nH=$sgId`nNm=$($pin.Name)`n]`n"
        $dpProp = if ($pin.Tp -eq 2) { "Trg=2" } elseif ($pin.Tp -eq 4 -or $pin.Tp -eq 5) { "NF=63`nDNF=1" } elseif ($pin.Tp -eq 1) { "NoS=FALSE`nEncFmt=0`nDVLF=1`nSgn=0" } else { "" }
        $dpPropStr = if ($dpProp) { "`n$dpProp" } else { "" }
        $dpBlocks += "[`nObjTp=Dp`nH=$dpId`nTp=$($pin.Tp)`nSD=$($pin.Name)$dpPropStr`n]`n"
        $sgId++; $dpId++
    }

    $content = @"
[
Version=1
]
[
ObjTp=FSgntr
Sgntr=UserMacro
RelVrs=4.14.21
IntStrVrs=2
MinSMWVrs=4.14.0
MinTIOVrs=0
]
[
ObjTp=Hd
S0Nd=1
S1Nd=2
SLNd=3
PrNm=$umcName
McNm=$umcBaseName
SmVr=1115
DvVr=1115
TpN1=1
TpN2=2
TpN3=3
TpN4=4
TpN5=5
APg=1
FltTmp=1
FpCS=0
EnType=0
ZeroOnIoOk=0
SGMethod=1
"@

    for ($i=0; $i -lt $module.Comments.Count; $i++) {
        $content += "`nCmn$($i+1)=$($module.Comments[$i])"
    }
    
    $content += "`n]`n[`n"
    $content += @"
ObjTp=Symbol
Name=$umcBaseName
Code=1
SmplCName=$umcName
Smpl-C=3
CompilerTimePrecision=Single_Or_Double_Precision
Exclusions=1,19,20,21,88,89,167,168,179,213,214,215,216,217,225,226,248,249,266,267,310,362,378,380,405,407,408,409,478,522,537,554,586,590,611,624,718,756,767,830,841,842,854,883,955,1032,1062,1079,1128,1129,1134,1140,1157,1158,1195,1199,1220,1221,1222,1223,1299,1348,1349,1439,1472,1473,1499,1746,1803,1975,2229,2354,2514,2523,2532,2706,2707,3235,3236,3427,3454,3567,3568,3601,3602,3708,3902,3903,3912,3918,3925,3926,4206,4207,
SMWRev=4.14.0
TIORev=0
S+=1
HelpID=1055
APg=1
StdCmd=0
FltTmp=1
FpCS=0
NumFixedInputs=$($macroInputs.Count)
NumFixedOutputs=$($macroOutputs.Count)
NumFixedParams=$($macroParams.Count)
MinVariableInputs=0
MinVariableInputsList2=0
MinVariableInputsList3=0
MinVariableOutputs=0
MinVariableOutputsList2=0
MinVariableOutputsList3=0
MinVariableParams=0
SymbolTree=46
UserSymTreeName=$category
"@

    for ($i=0; $i -lt $macroInputs.Count; $i++) { 
        $t = if ($macroInputs[$i].IsGap) { "Digital|Analog|Serial|String" } elseif ($macroInputs[$i].SigType -eq "Digital") { "Digital" } elseif ($macroInputs[$i].SigType -eq "Analog") { "Analog" } else { "Serial" }
        $content += "`nInputSigType$($i+1)=$t"
    }
    for ($i=0; $i -lt $macroOutputs.Count; $i++) { 
        $t = if ($macroOutputs[$i].IsGap) { "Digital|Analog|Serial|String" } elseif ($macroOutputs[$i].SigType -eq "Digital") { "Digital" } elseif ($macroOutputs[$i].SigType -eq "Analog") { "Analog" } else { "Serial" }
        $content += "`nOutputSigType$($i+1)=$t" 
    }
    for ($i=0; $i -lt $macroParams.Count; $i++) { 
        $content += "`nParamSigType$($i+1)=String|Constant" 
    }

    for ($i=0; $i -lt $macroInputs.Count; $i++) { $content += "`nInputCue$($i+1)=$($macroInputs[$i].Name)" }
    for ($i=0; $i -lt $macroOutputs.Count; $i++) { $content += "`nOutputCue$($i+1)=$($macroOutputs[$i].Name)" }
    for ($i=0; $i -lt $macroParams.Count; $i++) { $content += "`nParamCue$($i+1)=$($macroParams[$i].Name)" }

    $validInputs = @($macroInputs | Where-Object { -not $_.IsGap })
    $validOutputs = @($macroOutputs | Where-Object { -not $_.IsGap })

    $mPi = $validInputs.Count
    $content += "`nMPi=$mPi"
    $idx = 1
    foreach ($pin in $validInputs) { $content += "`nPi$idx=$($pin.DpId)"; $idx++ }
    
    $mPo = $validOutputs.Count
    $content += "`nMPo=$mPo"
    $idx = 1
    foreach ($pin in $validOutputs) { $content += "`nPo$idx=$($pin.DpId)"; $idx++ }
    
    $mPp = $macroParams.Count
    $content += "`nMPp=$mPp"
    $idx = 1
    foreach ($pin in $macroParams) { $content += "`nPp$idx=$($pin.DpId)"; $idx++ }
    
    $content += "`nFileName=$umcName`nEncodingType=0`nZeroOnIoOk=0`n]`n"
    
    $content += "[`nObjTp=Sm`nH=1`nSmC=157`nNm=Central Control Modules`nObjVer=1`nCF=2`nn1I=1`nn1O=1`nmI=1`nmO=1`ntO=1`nmP=1`nP1=`n]`n"
    $content += "[`nObjTp=Sm`nH=2`nSmC=157`nNm=Network Modules`nObjVer=1`nCF=2`nn1I=1`nn1O=1`nmI=1`nmO=1`ntO=1`nmP=1`nP1=`n]`n"
    $content += "[`nObjTp=Sm`nH=3`nSmC=157`nNm=Ethernet`nObjVer=1`nCF=2`nn1I=1`nn1O=1`nmI=1`nmO=1`ntO=1`nmP=1`nP1=`n]`n"
    $content += "[`nObjTp=Sm`nH=4`nSmC=156`nNm=Logic`nObjVer=1`nCF=2`nmC=1`nC1=7`n]`n"
    $content += "[`nObjTp=Sm`nH=5`nSmC=157`nNm=DefineArguments`nObjVer=1`nCF=2`nn1I=1`nn1O=1`nmC=1`nC1=6`nmI=1`nmO=1`ntO=1`nmP=1`nP1=`n]`n"

    $content += "[`nObjTp=Sm`nH=6`nSmC=55`nNm=Argument Definition`nObjVer=1`nPrH=5`nCF=2"
    $content += "`nn1I=$($macroInputs.Count)`nn1O=$($macroOutputs.Count)"
    
    $content += "`nmI=$($macroInputs.Count)"
    foreach ($pin in $macroInputs) { $content += "`nI$($pin.MacroId)=$($pin.SgId)" }
    
    $content += "`nmO=$($macroOutputs.Count)`ntO=$($macroOutputs.Count)"
    foreach ($pin in $macroOutputs) { $content += "`nO$($pin.MacroId)=$($pin.SgId)" }
    
    $content += "`nmP=$($macroParams.Count)"
    foreach ($pin in $macroParams) { $content += "`nP$($pin.MacroId)=$($pin.Name)" }
    
    $content += "`nMPi=$mPi"
    $idx = 1
    foreach ($pin in $validInputs) { $content += "`nPi$idx=$($pin.DpId)"; $idx++ }
    $content += "`nMPo=$mPo"
    $idx = 1
    foreach ($pin in $validOutputs) { $content += "`nPo$idx=$($pin.DpId)"; $idx++ }
    $content += "`nMPp=$mPp"
    $idx = 1
    foreach ($pin in $macroParams) { $content += "`nPp$idx=$($pin.DpId)"; $idx++ }
    $content += "`n]`n"

    $content += "[`nObjTp=Sm`nH=7`nSmC=103`nNm=$($module.SmplCName)`nObjVer=1`nPrH=4`nCF=2"
    
    # Critical mappings for Digitals vs List2
    if ($module.MaxI -gt 0) { $content += "`nn1I=$($module.MaxI)" }
    if ($module.MaxI2 -gt 0) { $content += "`nn2I=$($module.MaxI2)" }
    if ($module.MaxO -gt 0) { $content += "`nn1O=$($module.MaxO)" }
    if ($module.MaxO2 -gt 0) { $content += "`nn2O=$($module.MaxO2)" }
    
    $content += "`nmI=$($module.MaxI + $module.MaxI2)"
    foreach ($pin in $macroInputs | Where-Object { -not $_.IsGap }) {
        $innerIndex = if ($pin.List -eq 1) { $pin.InnerId } else { $pin.InnerId + $module.MaxI }
        $content += "`nI$innerIndex=$($pin.SgId)"
    }
    
    $content += "`nmO=$($module.MaxO + $module.MaxO2)`ntO=$($module.MaxO + $module.MaxO2)"
    foreach ($pin in $macroOutputs | Where-Object { -not $_.IsGap }) {
        $innerIndex = if ($pin.List -eq 1) { $pin.InnerId } else { $pin.InnerId + $module.MaxO }
        $content += "`nO$innerIndex=$($pin.SgId)"
    }
    
    $content += "`nmP=$($module.TotalP)"
    for ($idx=1; $idx -le $module.TotalP; $idx++) {
        $pObj = $macroParams | Where-Object { $_.InnerId -eq $idx } | Select-Object -First 1
        if ($pObj) { $content += "`nP$idx=#$($pObj.Name)" }
        else { $content += "`nP$idx=" }
    }
    $content += "`n]`n"

    $content += $signalBlocks
    $content += $dpBlocks

    # Ensure CRLF line endings for legacy SIMPL Windows parsing
    $content = $content -replace "`r`n", "`n" # normalize just in case
    $content = $content -replace "`n", "`r`n"
    
    [System.IO.File]::WriteAllText($umcPath, $content, [System.Text.Encoding]::ASCII)
    Write-Host "Success: $umcPath" -ForegroundColor Green
}

foreach ($f in $ushFiles) {
    if ($f.Name -match "^Lockton") { continue }
    $mod = Parse-Ush $f.FullName
    Generate-Umc $mod
}
Write-Host "UMC Generation Complete!" -ForegroundColor Cyan
