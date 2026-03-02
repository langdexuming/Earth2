param(
    [string]$ProjectRoot = "",
    [string]$ExtractRoot = "",
    [string]$ModelOutputRoot = "",
    [string]$ReportRoot = "",
    [string]$TargetsFile = "",
    [string]$NoesisExe = "",
    [string]$AsciiWorkRoot = "C:\EE2NoesisWork",
    [ValidateSet("fbx", "obj")]
    [string]$OutputFormat = "fbx",
    [switch]$ClearOutput,
    [switch]$UseConsolelessMode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Resolve-ProjectPath {
    param([Parameter(Mandatory = $true)][string]$Base, [Parameter(Mandatory = $true)][string]$Relative)
    return [System.IO.Path]::GetFullPath((Join-Path $Base $Relative))
}

function Load-TargetPatterns {
    param([string]$Path)

    $fallback = @(
        "bld_citycenter13",
        "bld_barracks13",
        "bld_robotics_factory",
        "bld_airport13",
        "lhi13_assaultrifleman_we",
        "lhi13_assaultrifleman",
        "lhm13_mainbattletank",
        "AF13_JetFighter"
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $fallback
    }

    $rows = Import-Csv -Path $Path
    if ($rows -eq $null) {
        return $fallback
    }

    $patterns = New-Object System.Collections.Generic.List[string]
    foreach ($row in $rows) {
        if ($row -eq $null) {
            continue
        }

        $raw = $row.Pattern
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        $value = $raw.Trim()
        if ($value.Length -eq 0) {
            continue
        }

        if (-not $patterns.Contains($value)) {
            $patterns.Add($value)
        }
    }

    if ($patterns.Count -eq 0) {
        return $fallback
    }

    return $patterns.ToArray()
}

function Resolve-NoesisExePath {
    param([string]$PreferredPath)

    if (-not [string]::IsNullOrWhiteSpace($PreferredPath) -and (Test-Path $PreferredPath)) {
        return (Resolve-Path $PreferredPath).Path
    }

    $candidatePaths = @(
        "C:\Noesis\Noesis64.exe",
        "C:\Users\langd\AppData\Local\Microsoft\WinGet\Packages\RichWhitehouse.Noesis_Microsoft.Winget.Source_8wekyb3d8bbwe\Noesis64.exe"
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $alias = Get-Command noesis64 -ErrorAction SilentlyContinue
    if ($alias -and -not [string]::IsNullOrWhiteSpace($alias.Source)) {
        return $alias.Source
    }

    return $null
}

function Get-ConversionArgs {
    param(
        [Parameter(Mandatory = $true)][string]$Mode,
        [Parameter(Mandatory = $true)][string]$InputPath,
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [Parameter(Mandatory = $true)][string]$LogPath,
        [Parameter(Mandatory = $true)][string]$Format
    )

    $args = New-Object System.Collections.Generic.List[string]
    $args.Add($Mode)
    $args.Add($InputPath)
    $args.Add($OutputPath)

    switch ($Format) {
        "fbx" {
            $args.Add("-fbxnewexport")
            $args.Add("-fbxtexrel")
        }
        "obj" {
            $args.Add("-objmtl")
        }
    }

    $args.Add("-logfile")
    $args.Add($LogPath)
    return $args.ToArray()
}

function Get-ReasonFromLog {
    param([string]$LogPath)

    if (-not (Test-Path $LogPath)) {
        return "No log file."
    }

    $tail = Get-Content -Path $LogPath -Tail 8
    if ($tail -eq $null -or $tail.Count -eq 0) {
        return "Log is empty."
    }

    return ($tail -join " | ")
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}
if ([string]::IsNullOrWhiteSpace($ExtractRoot)) {
    $ExtractRoot = Resolve-ProjectPath -Base $ProjectRoot -Relative "ExternalAssets\EE2_Extracted"
}
if ([string]::IsNullOrWhiteSpace($ModelOutputRoot)) {
    $ModelOutputRoot = Resolve-ProjectPath -Base $ProjectRoot -Relative "Assets\External\EE2Converted\Models"
}
if ([string]::IsNullOrWhiteSpace($ReportRoot)) {
    $ReportRoot = Resolve-ProjectPath -Base $ProjectRoot -Relative "ExternalAssets\EE2_Converted"
}
if ([string]::IsNullOrWhiteSpace($TargetsFile)) {
    $TargetsFile = Resolve-ProjectPath -Base $ProjectRoot -Relative "Tools\ee2_convert_targets.csv"
}

$NoesisExe = Resolve-NoesisExePath -PreferredPath $NoesisExe
if ([string]::IsNullOrWhiteSpace($NoesisExe) -or -not (Test-Path $NoesisExe)) {
    throw "Noesis64.exe not found. Set -NoesisExe explicitly."
}

if (-not (Test-Path $ExtractRoot)) {
    throw "Extract root does not exist: $ExtractRoot"
}

$stageInRoot = Join-Path $AsciiWorkRoot "input"
$stageOutRoot = Join-Path $AsciiWorkRoot "output"
$logRoot = Join-Path $ReportRoot "logs"
$manifestPath = Join-Path $ReportRoot "convert_manifest.csv"

if ($ClearOutput) {
    if (Test-Path $ModelOutputRoot) {
        Remove-Item -Path $ModelOutputRoot -Recurse -Force
    }
    if (Test-Path $ReportRoot) {
        Remove-Item -Path $ReportRoot -Recurse -Force
    }
}

Ensure-Directory -Path $ModelOutputRoot
Ensure-Directory -Path $ReportRoot
Ensure-Directory -Path $stageInRoot
Ensure-Directory -Path $stageOutRoot
Ensure-Directory -Path $logRoot

$preferredAssetPatterns = Load-TargetPatterns -Path $TargetsFile

$graphicsRoot = Join-Path $ExtractRoot "graphics"
if (-not (Test-Path $graphicsRoot)) {
    throw "Missing extracted graphics folder: $graphicsRoot"
}

$allCandidates = Get-ChildItem -Path $graphicsRoot -Recurse -File | Where-Object {
    $_.Extension.Equals(".nif", [System.StringComparison]::OrdinalIgnoreCase) -or
    $_.Extension.Equals(".nifcache", [System.StringComparison]::OrdinalIgnoreCase)
}

$selected = New-Object System.Collections.Generic.List[System.IO.FileInfo]
foreach ($pattern in $preferredAssetPatterns) {
    $patternLower = $pattern.ToLowerInvariant()
    $match = $allCandidates |
        Where-Object { $_.Name.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 } |
        Sort-Object @{
            Expression = {
                $score = 0
                $base = $_.BaseName.ToLowerInvariant()

                if ($base -eq $patternLower) {
                    $score -= 200
                }
                elseif ($base.StartsWith($patternLower + "_", [System.StringComparison]::Ordinal)) {
                    $score -= 40
                }

                if ($base.StartsWith("fx_", [System.StringComparison]::Ordinal)) {
                    $score += 120
                }
                if ($base.IndexOf("damage", [System.StringComparison]::Ordinal) -ge 0) {
                    $score += 80
                }
                if ($base.IndexOf("wreck", [System.StringComparison]::Ordinal) -ge 0) {
                    $score += 60
                }

                if ($_.Extension.Equals(".nifcache", [System.StringComparison]::OrdinalIgnoreCase)) {
                    $score += 5
                }

                return $score
            }
        }, Length, Name |
        Select-Object -First 1

    if ($match -ne $null) {
        $selected.Add($match)
    }
}

# Deduplicate by full path.
$selectedPaths = @($selected | Select-Object -ExpandProperty FullName -Unique)

if ($selectedPaths.Count -eq 0) {
    throw "No target NIF files found under: $graphicsRoot"
}

$modeOrder = if ($UseConsolelessMode) { @("?cmode_nocon", "?cmode") } else { @("?cmode", "?cmode_nocon") }
$outputExt = "." + $OutputFormat
$results = New-Object System.Collections.Generic.List[object]

Write-Output "Noesis: $NoesisExe"
Write-Output "Input files: $($selectedPaths.Count)"
Write-Output "Target patterns: $($preferredAssetPatterns.Count)"
Write-Output "Model output: $ModelOutputRoot"
Write-Output "Report output: $ReportRoot"

foreach ($sourcePath in $selectedPaths) {
    $sourceFile = Get-Item -Path $sourcePath
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($sourceFile.Name)
    $stageInputPath = Join-Path $stageInRoot ($baseName + ".nif")
    $stageOutputPath = Join-Path $stageOutRoot ($baseName + $outputExt)
    $finalOutputPath = Join-Path $ModelOutputRoot ($baseName + $outputExt)
    $logPath = Join-Path $logRoot ($baseName + ".log")

    Copy-Item -Path $sourcePath -Destination $stageInputPath -Force

    $success = $false
    $usedMode = ""
    $exitCode = -1
    $reason = ""

    foreach ($mode in $modeOrder) {
        Remove-Item -Path $stageOutputPath -ErrorAction SilentlyContinue
        Remove-Item -Path $finalOutputPath -ErrorAction SilentlyContinue
        Remove-Item -Path $logPath -ErrorAction SilentlyContinue

        $args = Get-ConversionArgs -Mode $mode -InputPath $stageInputPath -OutputPath $stageOutputPath -LogPath $logPath -Format $OutputFormat
        $process = Start-Process -FilePath $NoesisExe -ArgumentList $args -WorkingDirectory (Split-Path -Parent $NoesisExe) -PassThru -Wait
        $exitCode = $process.ExitCode

        if (Test-Path $stageOutputPath) {
            $size = (Get-Item -Path $stageOutputPath).Length
            if ($size -gt 0) {
                Copy-Item -Path $stageOutputPath -Destination $finalOutputPath -Force
                $success = $true
                $usedMode = $mode
                $reason = "Converted"
                break
            }
        }

        $usedMode = $mode
        $reason = Get-ReasonFromLog -LogPath $logPath
    }

    $results.Add([PSCustomObject]@{
        Name = $baseName
        Source = $sourcePath
        StageInput = $stageInputPath
        Output = $finalOutputPath
        Format = $OutputFormat
        Success = $success
        Mode = $usedMode
        ExitCode = $exitCode
        LogPath = $logPath
        Reason = $reason
    })
}

$results | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $manifestPath

$successCount = @($results | Where-Object { $_.Success }).Count
$failCount = $results.Count - $successCount
$unknownCount = @($results | Where-Object { -not $_.Success -and $_.Reason.IndexOf("Detected file type: Unknown", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 }).Count

Write-Output "Conversion complete."
Write-Output "Success: $successCount"
Write-Output "Failed: $failCount"
Write-Output "Manifest: $manifestPath"
Write-Output "Unity models: $ModelOutputRoot"

if ($unknownCount -gt 0) {
    Write-Warning "Some files were not recognized by Noesis. If GUI shows Python init errors, run Noesis from an ASCII-only path (for example C:\Noesis) and avoid Chinese characters in the current directory."
}
