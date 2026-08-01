<#
.SYNOPSIS
    Release helper: reads VERSION, promotes CHANGELOG, stamps the MSIX manifest.

.DESCRIPTION
    The release logic lives here rather than inline in the workflow YAML for one
    reason: this can be run locally. A script pasted into a workflow step is first
    executed in anger on the release commit itself.

.EXAMPLE
    pwsh tools/Release.ps1 -Command version
    pwsh tools/Release.ps1 -Command check
    pwsh tools/Release.ps1 -Command notes -OutFile RELEASE_NOTES.md
    pwsh tools/Release.ps1 -Command release -Date 2026-08-01 -OutFile RELEASE_NOTES.md
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('version', 'check', 'notes', 'release', 'stamp-manifest')]
    [string]$Command,

    [string]$Date,
    [string]$OutFile,
    [string]$Root
)

$ErrorActionPreference = 'Stop'

# Ustalane w ciele, nie w domyślnej wartości parametru: $PSScriptRoot nie jest tam
# jeszcze wypełniony w Windows PowerShell 5.1.
if (-not $Root) { $Root = Split-Path -Parent $PSScriptRoot }

$VersionFile   = Join-Path $Root 'VERSION'
$ChangelogFile = Join-Path $Root 'CHANGELOG.md'
$ManifestFile  = Join-Path $Root 'AIDemon2.Package/Package.appxmanifest'
$Unreleased    = '## [Unreleased]'

function Get-ProjectVersion {
    if (-not (Test-Path $VersionFile)) { throw "VERSION file not found at $VersionFile" }

    $value = (Get-Content $VersionFile -Raw -Encoding utf8).Trim()
    if ($value -notmatch '^\d+\.\d+\.\d+$') {
        throw "VERSION must be MAJOR.MINOR.PATCH (no 'v', no fourth part), got '$value'"
    }
    return $value
}

function Get-SectionBody {
    param([string[]]$Lines, [int]$Start)

    $body = @()
    for ($i = $Start + 1; $i -lt $Lines.Count; $i++) {
        # A link-definition block ends the section just like the next heading does;
        # without this the last section swallows the reference links.
        if ($Lines[$i] -match '^## \[' -or $Lines[$i] -match '^\[[^\]]+\]:\s') { break }
        $body += $Lines[$i]
    }
    return ($body -join "`n").Trim()
}

function Get-ReleaseNotes {
    param([string]$Version)

    if (-not (Test-Path $ChangelogFile)) { throw "CHANGELOG.md not found at $ChangelogFile" }
    # -Encoding utf8: Windows PowerShell 5.1 czyta domyslnie w stronie kodowej ANSI
    # i rozsypuje polskie znaki oraz myslniki w notatkach wydania.
    $lines = Get-Content $ChangelogFile -Encoding utf8

    # An already-written section for this version wins: makes the whole thing
    # idempotent and lets a release be described by hand when that is clearer.
    $existing = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l.StartsWith("## [$Version]") })
    if ($existing -ge 0) {
        $body = Get-SectionBody -Lines $lines -Start $existing
        if (-not $body) { throw "CHANGELOG.md has an empty section for $Version - describe the release" }
        return @{ Notes = $body; Changelog = $null }
    }

    $unreleasedIndex = [Array]::IndexOf($lines, $Unreleased)
    if ($unreleasedIndex -lt 0) { throw "CHANGELOG.md is missing the '$Unreleased' section" }

    $body = Get-SectionBody -Lines $lines -Start $unreleasedIndex
    if (-not $body) {
        throw "Nothing to release: the '$Unreleased' section of CHANGELOG.md is empty"
    }

    return @{ Notes = $body; Changelog = $lines; UnreleasedIndex = $unreleasedIndex }
}

function Invoke-ChangelogPromotion {
    param([string]$Version, [string]$ReleaseDate)

    $result = Get-ReleaseNotes -Version $Version
    if ($null -eq $result.Changelog) { return $result.Notes }   # already promoted

    $lines = $result.Changelog
    $index = $result.UnreleasedIndex

    $bodyEnd = $index + 1
    while ($bodyEnd -lt $lines.Count -and
           $lines[$bodyEnd] -notmatch '^## \[' -and
           $lines[$bodyEnd] -notmatch '^\[[^\]]+\]:\s') { $bodyEnd++ }

    $head = if ($index -gt 0) { $lines[0..($index - 1)] } else { @() }
    $tail = if ($bodyEnd -lt $lines.Count) { $lines[$bodyEnd..($lines.Count - 1)] } else { @() }

    $previous = $null
    foreach ($line in $tail) {
        if ($line -match '^## \[(\d+\.\d+\.\d+)\]') { $previous = $Matches[1]; break }
    }

    $repo = 'https://github.com/Mysttic/AIDemon2'
    $newLines = @()
    $newLines += $head
    $newLines += $Unreleased
    $newLines += ''
    $newLines += "## [$Version] - $ReleaseDate"
    $newLines += ''
    $newLines += $result.Notes -split "`n"
    $newLines += ''
    $newLines += $tail

    # Refresh the reference links at the bottom.
    $output = @()
    $unreleasedLink = "[Unreleased]: $repo/compare/v$Version...HEAD"
    $versionLink = if ($previous) { "[$Version]: $repo/compare/v$previous...v$Version" }
                   else           { "[$Version]: $repo/releases/tag/v$Version" }

    $linkWritten = $false
    foreach ($line in $newLines) {
        if ($line -match '^\[Unreleased\]:') {
            $output += $unreleasedLink
            $output += $versionLink
            $linkWritten = $true
        }
        else { $output += $line }
    }
    if (-not $linkWritten) {
        $output += ''
        $output += $unreleasedLink
        $output += $versionLink
    }

    ($output -join "`n").TrimEnd() + "`n" | Set-Content $ChangelogFile -NoNewline -Encoding utf8
    return $result.Notes
}

function Set-ManifestVersion {
    param([string]$Version)

    if (-not (Test-Path $ManifestFile)) { throw "Package.appxmanifest not found at $ManifestFile" }

    # MSIX versions are Major.Minor.Build.Revision and the Store requires
    # Revision to be 0, so the SemVer patch maps onto Build.
    $msixVersion = "$Version.0"
    [xml]$manifest = Get-Content $ManifestFile -Raw -Encoding utf8
    $manifest.Package.Identity.Version = $msixVersion
    $manifest.Save((Resolve-Path $ManifestFile))
    return $msixVersion
}

function Write-Output-To {
    param([string]$Text, [string]$Path)

    if (-not $Path) { Write-Output $Text; return }

    # WriteAllText z jawnym UTF8Encoding($false), a nie Set-Content -Encoding utf8:
    # w Windows PowerShell 5.1 to drugie dopisuje BOM, który wylądowałby jako
    # widoczny śmieć na początku opisu wydania na GitHubie.
    $full = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
    [System.IO.File]::WriteAllText($full, $Text, [System.Text.UTF8Encoding]::new($false))
}

switch ($Command) {
    'version' {
        Write-Output (Get-ProjectVersion)
    }
    'check' {
        $version = Get-ProjectVersion
        $null = Get-ReleaseNotes -Version $version
        if (-not (Test-Path $ManifestFile)) { throw "Package.appxmanifest not found" }
        Write-Output "OK: version $version, CHANGELOG ready, manifest present"
    }
    'notes' {
        $version = Get-ProjectVersion
        Write-Output-To -Text (Get-ReleaseNotes -Version $version).Notes -Path $OutFile
    }
    'stamp-manifest' {
        $version = Get-ProjectVersion
        Write-Output (Set-ManifestVersion -Version $version)
    }
    'release' {
        if (-not $Date) { throw "The 'release' command requires -Date yyyy-MM-dd" }
        $version = Get-ProjectVersion
        $notes = Invoke-ChangelogPromotion -Version $version -ReleaseDate $Date
        $null = Set-ManifestVersion -Version $version
        Write-Output-To -Text $notes -Path $OutFile
    }
}
