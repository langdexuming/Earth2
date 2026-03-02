param(
    [string]$EE2Root = "F:\BaiduNetdiskDownload\earth2",
    [string]$OutputRoot = "D:\UnityCode\Earth2\ExternalAssets\EE2_Extracted",
    [switch]$ClearOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Matches-AnyPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string[]]$Patterns
    )
    foreach ($pattern in $Patterns) {
        if ($Text.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }
    return $false
}

function Write-BytesToFile {
    param(
        [Parameter(Mandatory = $true)][byte[]]$Bytes,
        [Parameter(Mandatory = $true)][string]$Path
    )
    $parent = Split-Path -Parent $Path
    Ensure-Directory -Path $parent
    [System.IO.File]::WriteAllBytes($Path, $Bytes)
}

function Get-AlternatePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $ext = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()
    $dir = Split-Path -Parent $Path
    $base = [System.IO.Path]::GetFileNameWithoutExtension($Path)

    switch ($ext) {
        ".nifcache" { return (Join-Path $dir ($base + ".nif")) }
        ".texcache" { return (Join-Path $dir ($base + ".tex.nif")) }
        ".pcpatch" { return (Join-Path $dir ($base + ".pcpatch.nif")) }
        default { return $null }
    }
}

function Extract-FromZip {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$OutputFolder,
        [Parameter(Mandatory = $true)][string[]]$Patterns,
        [Parameter(Mandatory = $true)][string]$Tag,
        [ref]$Manifest
    )

    Ensure-Directory -Path $OutputFolder
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $zip.Entries) {
            if ([string]::IsNullOrWhiteSpace($entry.Name)) {
                continue
            }

            if (-not (Matches-AnyPattern -Text $entry.FullName -Patterns $Patterns)) {
                continue
            }

            $targetPath = Join-Path $OutputFolder $entry.FullName
            Ensure-Directory -Path (Split-Path -Parent $targetPath)

            $stream = $entry.Open()
            try {
                $memory = New-Object System.IO.MemoryStream
                try {
                    $stream.CopyTo($memory)
                    $bytes = $memory.ToArray()
                    Write-BytesToFile -Bytes $bytes -Path $targetPath

                    $alt = Get-AlternatePath -Path $targetPath
                    if ($alt) {
                        Write-BytesToFile -Bytes $bytes -Path $alt
                    }

                    $Manifest.Value.Add([PSCustomObject]@{
                        SourceZip = $ZipPath
                        Entry = $entry.FullName
                        ExtractedPath = $targetPath
                        AlternatePath = $alt
                        Group = $Tag
                        SizeBytes = $entry.Length
                    })
                }
                finally {
                    $memory.Dispose()
                }
            }
            finally {
                $stream.Dispose()
            }
        }
    }
    finally {
        $zip.Dispose()
    }
}

$graphicsZip = Join-Path $EE2Root "zips\graphics.zip"
$texturesZip = Join-Path $EE2Root "zips\textures.zip"

if (-not (Test-Path $graphicsZip)) {
    throw "Missing graphics.zip: $graphicsZip"
}
if (-not (Test-Path $texturesZip)) {
    throw "Missing textures.zip: $texturesZip"
}

if ($ClearOutput -and (Test-Path $OutputRoot)) {
    Remove-Item -Path $OutputRoot -Recurse -Force
}

Ensure-Directory -Path $OutputRoot

# Modern-style sample set: HQ, Barracks, Factory, Airfield, Infantry, Tank, Fighter.
$graphicsPatterns = @(
    "bld_citycenter13",
    "bld_barracks13",
    "bld_robotics_factory",
    "bld_airport13",
    "lhi13_assaultrifleman_we",
    "lhm13_mainbattletank",
    "AF13_JetFighter",
    "lhi13_assaultrifleman_",
    "Graphics/Building/bld_citycenter13",
    "Graphics/Building/bld_barracks13",
    "Graphics/Building/bld_airport13",
    "Graphics/Building/bld_robotics_factory",
    "Graphics/Humans/lhi13_assaultrifleman",
    "Graphics/Vehicles/lhm13_mainbattletank",
    "Graphics/Air/AF13_JetFighter"
)

$texturePatterns = @(
    "bld_citycenter13",
    "bld_barracks13",
    "bld_robotics_factory",
    "bld_airport13",
    "lhi13_assaultrifleman",
    "lhm13_mainbattletank",
    "AF13_JetFighter"
)

$manifest = New-Object System.Collections.Generic.List[object]

$graphicsOut = Join-Path $OutputRoot "graphics"
$texturesOut = Join-Path $OutputRoot "textures"

Extract-FromZip -ZipPath $graphicsZip -OutputFolder $graphicsOut -Patterns $graphicsPatterns -Tag "graphics" -Manifest ([ref]$manifest)
Extract-FromZip -ZipPath $texturesZip -OutputFolder $texturesOut -Patterns $texturePatterns -Tag "textures" -Manifest ([ref]$manifest)

$manifestPath = Join-Path $OutputRoot "extract_manifest.csv"
$manifest | Sort-Object Group, Entry | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $manifestPath

$summary = $manifest | Group-Object Group | Select-Object Name, Count
Write-Output "Extraction complete."
Write-Output "Output: $OutputRoot"
Write-Output "Manifest: $manifestPath"
Write-Output "Summary:"
$summary | Format-Table -AutoSize | Out-String | Write-Output
