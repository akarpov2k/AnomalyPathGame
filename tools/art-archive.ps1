#nullable disable
<#
.SYNOPSIS
Архив используемых ассетов AnomalyPath с быстрым восстановлением.

.DESCRIPTION
Снимает zip-архив всех PNG из Assets/Art вместе с их .meta и манифестом
(sha256, размер, разрешение, id каталога спрайтов), проверяет архив по манифесту
и возвращает файлы на место.

Архив не трогает GUID: .meta пакуется и возвращается рядом с PNG, поэтому ссылки
префабов и сцены остаются прежними. Каталог спрайтов после восстановления
пересобирать не нужно.

.PARAMETER Action
Snapshot — снять архив; Verify — сверить живые файлы с архивом; Restore — вернуть файлы.

.PARAMETER Archive
Путь к zip. Без него берётся самый свежий архив из -OutDir.

.PARAMETER Tag
Хвост имени архива и манифеста: art-<date>-<commit>-<Tag>.zip. Нужен, когда снимок делается повторно на том же коммите.

.PARAMETER OutDir
Папка архивов (по умолчанию art-archive/ в корне проекта).

.PARAMETER Force
Restore перезаписывает файл, изменённый после снятия архива.

.EXAMPLE
powershell -File tools/art-archive.ps1 -Action Snapshot
powershell -File tools/art-archive.ps1 -Action Verify
powershell -File tools/art-archive.ps1 -Action Restore -Force
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Snapshot', 'Verify', 'Restore')]
    [string] $Action,

    [string] $Archive,
    [string] $Tag,
    [string] $OutDir = 'art-archive',
    [string] $Root,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not $Root) {
    $Root = Split-Path -Parent $PSScriptRoot
}
$Root = (Resolve-Path $Root).Path

$ArtRelative = 'Assets/Art'
$ArtRoot = Join-Path $Root $ArtRelative
$ArchiveRoot = Join-Path $Root $OutDir
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Get-FileSha256([string] $path) {
    return (Get-FileHash -Path $path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-ImageSize([string] $path) {
    try {
        $image = [System.Drawing.Image]::FromFile($path)
        try {
            return @{ Width = $image.Width; Height = $image.Height }
        }
        finally {
            $image.Dispose()
        }
    }
    catch {
        return @{ Width = 0; Height = 0 }
    }
}

function Get-CatalogId([string] $relativePath) {
    $path = $relativePath -replace '\\', '/'
    $name = [System.IO.Path]::GetFileNameWithoutExtension($path)
    switch ($path) {
        { $_ -like 'Assets/Art/Icons/*' } { return "icons/$name" }
        { $_ -like 'Assets/Art/Backdrops/*' } { return "backdrops/$name" }
        { $_ -like 'Assets/Art/Portraits/*' } { return "portraits/$name" }
        { $_ -like 'Assets/Art/Buttons/*' } { return "buttons/$name" }
        { $_ -like 'Assets/Art/Costumes/*' } { return "costumes/$name" }
        { $_ -like 'Assets/Art/Quest/*' } { return "quests/$name" }
        { $_ -like 'Assets/Art/Backgrounds/*' } { return $name }
        { $_ -like 'Assets/Art/Locations/*' } { return $name }
    }
    return $null
}

function Get-GitInfo {
    $git = 'C:\Program Files\Git\cmd\git.exe'
    if (-not (Test-Path $git)) {
        return @{ Commit = $null; Branch = $null }
    }
    Push-Location $Root
    try {
        $commit = & $git rev-parse --short HEAD 2>$null
        $branch = & $git rev-parse --abbrev-ref HEAD 2>$null
        return @{ Commit = "$commit"; Branch = "$branch" }
    }
    finally {
        Pop-Location
    }
}

function Read-Manifest([string] $path) {
    return Get-Content -Path $path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Find-LatestArchive {
    if ($Archive) {
        if (-not (Test-Path $Archive)) {
            throw "Архив не найден: $Archive"
        }
        return (Resolve-Path $Archive).Path
    }
    if (-not (Test-Path $ArchiveRoot)) {
        throw "Нет папки архивов $ArchiveRoot — снимите архив: -Action Snapshot"
    }
    $latest = Get-ChildItem -Path $ArchiveRoot -Filter 'art-*.zip' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) {
        throw "В $ArchiveRoot нет ни одного art-*.zip — снимите архив: -Action Snapshot"
    }
    return $latest.FullName
}

function Invoke-Snapshot {
    if (-not (Test-Path $ArtRoot)) {
        throw "Нет папки $ArtRoot"
    }

    $stamp = Get-Date -Format 'yyyy-MM-dd'
    $git = Get-GitInfo
    if ($git.Commit) {
        $stamp = "$stamp-$($git.Commit)"
    }

    if ($Tag) {
        $stamp = "$stamp-$Tag"
    }

    New-Item -ItemType Directory -Force -Path $ArchiveRoot | Out-Null
    $staging = Join-Path $ArchiveRoot ('.staging-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    $entries = New-Object System.Collections.ArrayList
    $missingMeta = New-Object System.Collections.ArrayList
    $totalBytes = 0

    foreach ($file in Get-ChildItem -Path $ArtRoot -Recurse -File -Filter '*.png' | Sort-Object FullName) {
        $relative = $file.FullName.Substring($Root.Length).TrimStart('\', '/') -replace '\\', '/'
        $metaPath = $file.FullName + '.meta'
        $metaRelative = $relative + '.meta'

        $target = Join-Path $staging ($relative -replace '/', '\')
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -Path $file.FullName -Destination $target

        if (Test-Path $metaPath) {
            Copy-Item -Path $metaPath -Destination ($target + '.meta')
        }
        else {
            [void] $missingMeta.Add($relative)
        }

        $size = Get-ImageSize $file.FullName
        $bytes = $file.Length
        $totalBytes += $bytes

        [void] $entries.Add([ordered] @{
                path      = $relative
                meta      = $metaRelative
                hasMeta   = (Test-Path $metaPath)
                catalogId = (Get-CatalogId $relative)
                sha256    = (Get-FileSha256 $file.FullName)
                bytes     = $bytes
                width     = $size.Width
                height    = $size.Height
            })
    }

    $manifest = [ordered] @{
        tool      = 'tools/art-archive.ps1'
        project   = 'AnomalyPath'
        created   = (Get-Date -Format 'o')
        gitCommit = $git.Commit
        gitBranch = $git.Branch
        fileCount = $entries.Count
        totalBytes = $totalBytes
        files     = $entries
    }

    $manifestPath = Join-Path $staging 'manifest.json'
    [System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 6), $Utf8NoBom)
    Copy-Item -Path $manifestPath -Destination (Join-Path $ArchiveRoot "manifest-$stamp.json")

    $zip = Join-Path $ArchiveRoot "art-$stamp.zip"
    if (Test-Path $zip) {
        throw "Архив уже есть: $zip. Повторное снятие не перезаписывает архив — удалите его руками, если он не нужен."
    }

    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -CompressionLevel Optimal
    Remove-Item -Recurse -Force $staging

    Write-Host "Архив: $zip"
    Write-Host "Манифест: $(Join-Path $ArchiveRoot "manifest-$stamp.json")"
    Write-Host ("Файлов: {0}, PNG: {1:N1} МБ" -f $entries.Count, ($totalBytes / 1MB))
    if ($missingMeta.Count -gt 0) {
        Write-Warning ("Без .meta: {0}" -f ($missingMeta -join ', '))
    }
}

function Expand-ArchiveToTemp([string] $zipPath) {
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ('anomalypath-art-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $temp | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $temp -Force
    return $temp
}

function Compare-WithManifest([string] $staging) {
    $manifest = Read-Manifest (Join-Path $staging 'manifest.json')
    $ok = 0
    $missing = New-Object System.Collections.ArrayList
    $different = New-Object System.Collections.ArrayList

    foreach ($entry in $manifest.files) {
        $live = Join-Path $Root ($entry.path -replace '/', '\')
        if (-not (Test-Path $live)) {
            [void] $missing.Add($entry.path)
            continue
        }
        if ((Get-FileSha256 $live) -eq $entry.sha256) {
            $ok++
        }
        else {
            [void] $different.Add($entry.path)
        }
    }

    return @{
        Manifest  = $manifest
        Ok        = $ok
        Missing   = $missing
        Different = $different
    }
}

function Invoke-Verify {
    $zip = Find-LatestArchive
    $temp = Expand-ArchiveToTemp $zip
    try {
        $result = Compare-WithManifest $temp
        Write-Host "Архив: $zip"
        Write-Host ("Совпадает: {0} из {1}" -f $result.Ok, $result.Manifest.files.Count)
        foreach ($item in $result.Missing) { Write-Host "  нет: $item" }
        foreach ($item in $result.Different) { Write-Host "  отличается: $item" }
        if ($result.Missing.Count -gt 0 -or $result.Different.Count -gt 0) {
            exit 1
        }
    }
    finally {
        Remove-Item -Recurse -Force $temp -ErrorAction SilentlyContinue
    }
}

function Invoke-Restore {
    $zip = Find-LatestArchive
    $temp = Expand-ArchiveToTemp $zip
    try {
        $manifest = Read-Manifest (Join-Path $temp 'manifest.json')
        $created = [datetime]::Parse($manifest.created, [System.Globalization.CultureInfo]::InvariantCulture)
        $restored = 0
        $same = 0
        $blocked = New-Object System.Collections.ArrayList

        foreach ($entry in $manifest.files) {
            $source = Join-Path $temp ($entry.path -replace '/', '\')
            $live = Join-Path $Root ($entry.path -replace '/', '\')
            if (-not (Test-Path $source)) {
                throw "В архиве нет $($entry.path)"
            }

            if (Test-Path $live) {
                if ((Get-FileSha256 $live) -eq $entry.sha256) {
                    $same++
                    continue
                }
                if ((Get-Item $live).LastWriteTime -gt $created -and -not $Force) {
                    [void] $blocked.Add($entry.path)
                    continue
                }
            }

            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $live) | Out-Null
            Copy-Item -Path $source -Destination $live -Force
            if ($entry.hasMeta -and (Test-Path ($source + '.meta'))) {
                Copy-Item -Path ($source + '.meta') -Destination ($live + '.meta') -Force
            }
            $restored++
        }

        Write-Host "Архив: $zip"
        Write-Host ("Восстановлено: {0}, без изменений: {1}" -f $restored, $same)
        foreach ($item in $blocked) {
            Write-Host "  изменён после архива, пропущен: $item"
        }
        if ($blocked.Count -gt 0) {
            Write-Host "  повторить с -Force, если эти файлы можно перезаписать"
            exit 1
        }

        $result = Compare-WithManifest $temp
        if ($result.Missing.Count -gt 0 -or $result.Different.Count -gt 0) {
            Write-Host ("После восстановления расходятся: {0}" -f ($result.Missing.Count + $result.Different.Count))
            exit 1
        }
        Write-Host "Сверка после восстановления: всё совпадает"
    }
    finally {
        Remove-Item -Recurse -Force $temp -ErrorAction SilentlyContinue
    }
}

switch ($Action) {
    'Snapshot' { Invoke-Snapshot }
    'Verify' { Invoke-Verify }
    'Restore' { Invoke-Restore }
}
