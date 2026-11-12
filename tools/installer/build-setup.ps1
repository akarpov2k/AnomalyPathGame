<#
.SYNOPSIS
    Builds the Windows distribution and setup.exe for AnomalyPath.

.DESCRIPTION
    Steps:
      1) copies the fresh Unity build from Build\Windows to Builds\AnomalyPath-Windows
         (Unity's "BackUpThisFolder" directory is excluded) and adds README.txt;
      2) packs the folder into Builds\AnomalyPath-Windows.zip;
      3) compiles Uninstall.exe and setup.exe (AnomalyPath-Setup.exe) for .NET Framework 4.8
         with the Roslyn compiler from the .NET SDK; the poster, the game archive and the
         uninstaller are embedded into setup.exe (single-file installer);
      4) prints sizes and SHA256 of the deliverables.

    The Unity build must exist first:
      Unity.exe -batchmode -nographics -projectPath . -executeMethod AnomalyPath.Editor.BuildScript.BuildWindows -logFile Logs\Build.Windows.log

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\installer\build-setup.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\installer\build-setup.ps1 -SkipPayload
#>
[CmdletBinding()]
param(
    [string]$ProjectRoot = '',
    [string]$BuildDir = 'Build\Windows',
    [string]$DistName = 'AnomalyPath-Windows',
    [string]$OutDir = 'Builds',
    [string]$Poster = 'generated\poster_anomalypath_final_v2.png',
    [string]$Icon = 'generated\installer\icon.ico',
    [string]$DisplayName = '',
    [string]$Version = '',
    [string]$Publisher = '',
    [switch]$SkipPayload
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $ProjectRoot) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

function Write-Step([string]$Text) {
    Write-Host ''
    Write-Host "== $Text" -ForegroundColor Cyan
}

function Get-CscPath {
    $sdkRoot = Join-Path $env:ProgramFiles 'dotnet\sdk'
    if (-not (Test-Path $sdkRoot)) { throw "dotnet SDK not found: $sdkRoot" }
    $sdk = Get-ChildItem $sdkRoot -Directory | Sort-Object Name -Descending |
        Where-Object { Test-Path (Join-Path $_.FullName 'Roslyn\bincore\csc.dll') } |
        Select-Object -First 1
    if (-not $sdk) { throw 'Roslyn (csc.dll) not found in the .NET SDK' }
    return (Join-Path $sdk.FullName 'Roslyn\bincore\csc.dll')
}

function Get-FrameworkReferences {
    $root = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework'
    foreach ($version in @('v4.8', 'v4.7.2')) {
        $candidate = Join-Path $root $version
        if (Test-Path (Join-Path $candidate 'mscorlib.dll')) { return $candidate }
    }
    throw "reference assemblies not found: $root"
}

function Invoke-Csc([string]$Csc, [string]$Refs, [string]$Out, [string]$Manifest, [string]$IconPath, [string[]]$Sources, [string[]]$Resources) {
    $arguments = @(
        'exec', $Csc,
        '/nologo', '/noconfig', '/nostdlib+', '/target:winexe', '/platform:anycpu', '/optimize+',
        '/langversion:latest', '/codepage:65001', '/deterministic+',
        "/out:$Out", "/win32manifest:$Manifest"
    )
    if ($IconPath) { $arguments += "/win32icon:$IconPath" }
    foreach ($reference in @(
            'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll',
            'System.Windows.Forms.dll', 'System.IO.Compression.dll',
            'System.IO.Compression.FileSystem.dll', 'Microsoft.CSharp.dll')) {
        $arguments += "/r:$(Join-Path $Refs $reference)"
    }
    $arguments += $Sources
    $arguments += $Resources

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "csc exit code $LASTEXITCODE ($Out)" }
}

$root = (Resolve-Path $ProjectRoot).Path
$buildPath = Join-Path $root $BuildDir
$exe = Join-Path $buildPath 'AnomalyPath.exe'
if (-not (Test-Path $exe)) {
    throw "no Windows build: $exe (run AnomalyPath.Editor.BuildScript.BuildWindows first)"
}

# Version and publisher come from ProjectSettings unless overridden.
$projectSettings = Join-Path $root 'ProjectSettings\ProjectSettings.asset'
if (-not $Version) {
    $Version = (Select-String -Path $projectSettings -Pattern '^  bundleVersion: (.+)$').Matches[0].Groups[1].Value.Trim()
}
if (-not $Publisher) {
    $Publisher = (Select-String -Path $projectSettings -Pattern '^  companyName: (.+)$').Matches[0].Groups[1].Value.Trim()
}
if (-not $DisplayName) {
    $DisplayName = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'display-name.txt')).Trim()
}

$posterPath = Join-Path $root $Poster
$iconPath = Join-Path $root $Icon
$manifest = Join-Path $PSScriptRoot 'app.manifest'
$readme = Join-Path $PSScriptRoot 'README.install.txt'
$sources = @(Get-ChildItem (Join-Path $PSScriptRoot 'Core') -Filter *.cs | Sort-Object Name)
$installerSources = $sources + @(Get-ChildItem (Join-Path $PSScriptRoot 'Installer') -Filter *.cs | Sort-Object Name)
$uninstallerSources = $sources + @(Get-ChildItem (Join-Path $PSScriptRoot 'Uninstaller') -Filter *.cs | Sort-Object Name)
foreach ($file in @($posterPath, $iconPath, $manifest, $readme)) {
    if (-not (Test-Path $file)) { throw "missing file: $file" }
}

$stage = Join-Path $root '.build\installer\stage'
$dist = Join-Path (Join-Path $root $OutDir) $DistName
$zip = Join-Path (Join-Path $root $OutDir) "$DistName.zip"
$setup = Join-Path (Join-Path $root $OutDir) 'AnomalyPath-Setup.exe'

$rebuildPayload = (-not $SkipPayload) -or (-not (Test-Path $zip))
if ($rebuildPayload) {
    Write-Step "Distribution folder: $dist"
    if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $dist | Out-Null
    $skip = @('AnomalyPath_BackUpThisFolder_ButDontShipItWithYourGame')
    Get-ChildItem $buildPath -Force | Where-Object { $skip -notcontains $_.Name } | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $dist $_.Name) -Recurse -Force
    }

    $readmeText = [System.IO.File]::ReadAllText($readme) -replace "`r?`n", "`r`n"
    [System.IO.File]::WriteAllText((Join-Path $dist 'README.txt'), $readmeText, (New-Object System.Text.UTF8Encoding($true)))

    Write-Step 'Distribution archive'
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($dist, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
}
else {
    Write-Step 'Distribution archive: skipped (-SkipPayload)'
}

if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Force -Path $dist | Out-Null }
if (-not (Test-Path $stage)) { New-Item -ItemType Directory -Force -Path $stage | Out-Null }

$infoPath = Join-Path $stage 'setup.info'
$fileCount = (Get-ChildItem $dist -Recurse -File).Count
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipBytes = (Get-Item $zip).Length
$installedBytes = 0
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($entry in $archive.Entries) { $installedBytes += $entry.Length }
}
finally {
    $archive.Dispose()
}
$info = @(
    "app=AnomalyPath"
    "displayName=$DisplayName"
    "version=$Version"
    "publisher=$Publisher"
    "exe=AnomalyPath.exe"
    "files=$fileCount"
    "bytes=$installedBytes"
) -join "`r`n"
[System.IO.File]::WriteAllText($infoPath, $info + "`r`n", (New-Object System.Text.UTF8Encoding($true)))

Write-Step 'Compile Uninstall.exe'
$csc = Get-CscPath
$refs = Get-FrameworkReferences
$uninstallerOut = Join-Path $stage 'Uninstall.exe'
Invoke-Csc -Csc $csc -Refs $refs -Out $uninstallerOut -Manifest $manifest -IconPath $iconPath `
    -Sources ($uninstallerSources | ForEach-Object { $_.FullName }) `
    -Resources @("/resource:$posterPath,poster.png", "/resource:$infoPath,setup.info")

Write-Step 'Compile setup.exe'
Invoke-Csc -Csc $csc -Refs $refs -Out $setup -Manifest $manifest -IconPath $iconPath `
    -Sources ($installerSources | ForEach-Object { $_.FullName }) `
    -Resources @(
        "/resource:$zip,payload.zip",
        "/resource:$posterPath,poster.png",
        "/resource:$uninstallerOut,uninstall.exe",
        "/resource:$infoPath,setup.info"
    )

Write-Step 'Done'
$setupInfo = Get-Item $setup
$zipInfo = Get-Item $zip
$distBytes = (Get-ChildItem $dist -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host ('setup.exe : {0:N1} MB' -f ($setupInfo.Length / 1MB))
Write-Host ('zip       : {0:N1} MB' -f ($zipInfo.Length / 1MB))
Write-Host ('folder    : {0:N1} MB, {1} files' -f ($distBytes / 1MB), $fileCount)
Write-Host ('version   : {0} ({1})' -f $Version, $Publisher)

foreach ($file in @($setup, $zip)) {
    $hash = (Get-FileHash -Path $file -Algorithm SHA256).Hash
    Write-Host (('{0}  {1}' -f $hash, (Split-Path $file -Leaf)))
}
