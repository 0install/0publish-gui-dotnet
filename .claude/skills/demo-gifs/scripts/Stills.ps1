<#
.SYNOPSIS
Captures the stills of a flow (its $Stills: name -> script block calling Save-Still) to <OutDir>\<name>.png.
Uses window messages, UI Automation and PrintWindow only: no mouse, no keyboard, no screen reading, so other windows
may stay open and the user may keep working (0publish-gui's window does pop up on the primary monitor).

.PARAMETER Flow
Name of a flow file in ..\flows (without .ps1).
.PARAMETER Still
Capture only these stills (default: all).
#>
param(
    [Parameter(Mandatory)][string]$Flow,
    [string[]]$Still
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Config.ps1"
. "$PSScriptRoot\Helpers.ps1"
. "$PSScriptRoot\Steps.ps1"
. "$PSScriptRoot\..\flows\$Flow.ps1"
if (-not $Stills) { throw "Flow '$Flow' has no stills" }
New-Item -ItemType Directory -Force $OutDir | Out-Null

try {
    Initialize-Stage $Stage -Still
    [Gui]::SetForegroundWindow($script:MainHandle) | Out-Null
    foreach ($name in $(if ($Still) { $Still } else { @($Stills.Keys) })) {
        if (-not $Stills[$name]) { throw "Flow '$Flow' has no still '$name'" }
        & $Stills[$name]
    }
}
finally { Stop-Publish }
