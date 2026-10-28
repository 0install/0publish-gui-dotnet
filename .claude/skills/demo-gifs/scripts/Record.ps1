<#
.SYNOPSIS
Records the takes of a flow: for each take, starts a fresh 0publish-gui on stage, starts ffmpeg, runs the take's steps
and writes <OutDir>\recordings\<recording>\<take>.mkv plus <take>-events.json. Stops 0publish-gui afterwards.
Do not touch the mouse or keyboard while this runs.

.PARAMETER Flow
Name of a flow file in ..\flows (without .ps1).
.PARAMETER Take
Record only these takes (default: all takes of the flow, in order).
.PARAMETER Recording
Name of the recording directory (default: the flow name). Use a different name for rehearsals.
.PARAMETER AllowOverlap
Record even if other windows overlap the canvas (they will be visible in the video; for rehearsals only).
.PARAMETER ArchiveUrl
Overrides $App.ArchiveUrl, e.g. with a locally served copy to rehearse without downloading.
#>
param(
    [Parameter(Mandatory)][string]$Flow,
    [string[]]$Take,
    [string]$Recording,
    [switch]$AllowOverlap,
    [string]$ArchiveUrl
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Config.ps1"
. "$PSScriptRoot\Helpers.ps1"
. "$PSScriptRoot\Steps.ps1"
. "$PSScriptRoot\..\flows\$Flow.ps1"
if (-not $Takes) { throw "Flow '$Flow' has no takes" }
if ($ArchiveUrl) { $App.ArchiveUrl = $ArchiveUrl }

$dir = Join-Path $OutDir "recordings\$(if ($Recording) { $Recording } else { $Flow })"
New-Item -ItemType Directory -Force $dir | Out-Null

foreach ($name in $(if ($Take) { $Take } else { @($Takes.Keys) })) {
    if (-not $Takes[$name]) { throw "Flow '$Flow' has no take '$name'" }
    Initialize-Stage $Stage -AllowOverlap:$AllowOverlap
    Write-Host "Recording take '$name' to $dir"
    Reset-Events
    $ffmpeg = Start-Recording "$dir\$name.mkv"
    Log 'start'
    try {
        & $Takes[$name]
        Log 'end'
    }
    finally {
        Stop-Recording $ffmpeg
        Save-Events "$dir\$name-events.json"
        Stop-Publish
    }
    Show-Events
}
