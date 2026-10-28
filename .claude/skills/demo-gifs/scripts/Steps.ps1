# Building blocks for flows in 0publish-gui. Each step logs events that flows use for cutting
# (see SKILL.md for the list).

$script:Main = $null        # UIA element of the window on stage
$script:MainHandle = [IntPtr]::Zero
$script:Process = $null     # the 0publish-gui process started by Initialize-Stage

$ControlType = [System.Windows.Automation.ControlType]

#region Process
# Stops 0publish-gui instances started from $PublishExe (only ours: demo data, nothing worth saving). Other installations are left alone.
# Also removes the temp directories the wizard extracted archives to since our process started (killing it skips its cleanup).
function Stop-Publish {
    Get-Process -Name '0publish-gui' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $PublishExe } | Stop-Process -Force
    Start-Sleep -Milliseconds 500
    if ($script:Process) {
        Get-ChildItem $env:TEMP -Directory -Filter '0publish-*' | Where-Object { $_.CreationTime -ge $script:Process.StartTime } | Remove-Item -Recurse -Force
    }
}

# Starts 0publish-gui with the demo GnuPG home, in $WorkDir
function Start-Publish([string[]]$Arguments = @()) {
    if (-not (Test-Path $PublishExe)) { throw "$PublishExe not found; build it with src\build.ps1" }
    if (-not (Test-Path "$DemoGnuPGHome\secring.gpg")) { throw 'Demo GnuPG home missing; run scripts\Prepare.ps1' }
    Stop-Publish
    $psi = [Diagnostics.ProcessStartInfo]::new($PublishExe)
    $psi.Arguments = ($Arguments | ForEach-Object { "`"$_`"" }) -join ' '
    $psi.WorkingDirectory = $WorkDir
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables['GNUPGHOME'] = $DemoGnuPGHome
    $script:Process = [Diagnostics.Process]::Start($psi)
}

# Visible top-level windows of our 0publish-gui process (UIA elements; via EnumWindows, since UIA nests owned windows
# such as the wizard, progress dialogs and message boxes below their owner), except tooltips and $Exclude handles
function Get-ProcessWindows([int[]]$Exclude = @()) {
    [Gui]::ProcessWindows($script:Process.Id) | Where-Object { $Exclude -notcontains $_.ToInt32() } | ForEach-Object {
        try { $UIA::FromHandle($_) } catch {} # the window may close meanwhile
    } | Where-Object { $_ -and $_.Current.ClassName -notmatch 'tooltips' }
}

# Top-level window of our 0publish-gui process whose title matches
function Get-PublishWindow([string]$Title) {
    Get-ProcessWindows | Where-Object { $_.Current.Name -match $Title } | Select-Object -First 1
}
#endregion

#region Stage
# Returns visible top-level windows (not ours) that overlap the canvas and would show up in the recording
function Get-StageClutter {
    $c = $Crops.Canvas
    Get-TopWindows | Where-Object {
        $r = $_.Current.BoundingRectangle
        $_.Current.ProcessId -ne $script:Process.Id -and $_.Current.ClassName -ne 'Progman' -and -not $_.Current.IsOffscreen -and
        $r.X -lt $c.X + $c.W -and $r.X + $r.Width -gt $c.X -and $r.Y -lt $c.Y + $c.H -and $r.Y + $r.Height -gt $c.Y
    }
}

# Moves a window: to $Rect (X, Y, W, H) or, with -Center, keeping its size centered on $DialogCenter
function Move-Stage([IntPtr]$Handle, $Element, [hashtable]$Rect, [switch]$Center) {
    if ($Center) {
        $r = $Element.Current.BoundingRectangle
        [Gui]::MoveWindow($Handle, [int]($DialogCenter.X - $r.Width / 2), [int]($DialogCenter.Y - $r.Height / 2), [int]$r.Width, [int]$r.Height, $true) | Out-Null
    }
    else { [Gui]::MoveWindow($Handle, $Rect.X, $Rect.Y, $Rect.W, $Rect.H, $true) | Out-Null }
}

# Starts a fresh 0publish-gui before a take (not recorded) and puts its window on stage.
# $Stage: Arguments (command line, relative to $WorkDir), Title (window title regex), Window (rect) or Center ($true),
# optionally Setup (script block).
function Initialize-Stage([hashtable]$Stage, [switch]$AllowOverlap, [switch]$Still) {
    Start-Publish $Stage.Arguments
    $script:Main = Wait-Until { Get-PublishWindow $Stage.Title } 60
    $script:MainHandle = [IntPtr]$script:Main.Current.NativeWindowHandle
    Start-Sleep -Milliseconds 800 # let the editor finish loading
    Move-Stage $script:MainHandle $script:Main $Stage.Window -Center:$Stage.Center
    if ($Stage.Setup) { & $Stage.Setup } # unrecorded preparation without the mouse, e.g. expanding tree nodes
    if ($Still) { return } # stills use PrintWindow: overlapping windows and the cursor don't matter

    $clutter = @(Get-StageClutter)
    if ($clutter -and -not $AllowOverlap) {
        Stop-Publish
        throw "These windows overlap the recording area (screen x $($Crops.Canvas.X)-$($Crops.Canvas.X + $Crops.Canvas.W), y $($Crops.Canvas.Y)-$($Crops.Canvas.Y + $Crops.Canvas.H)); minimize them or move them to another monitor: " +
            (($clutter | ForEach-Object { "'$($_.Current.Name)'" }) -join ', ')
    }
    [Gui]::SetForegroundWindow($script:MainHandle) | Out-Null
    Start-Sleep -Milliseconds 500
    $r = $script:Main.Current.BoundingRectangle
    [Gui]::SetCursorPos([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height - 20)) | Out-Null # park inside the window, away from controls
    Start-Sleep -Seconds 1
    if ([Gui]::GetForegroundWindow() -ne $script:MainHandle) { throw '0publish-gui is not in the foreground' }
}
#endregion

#region Element lookup
# First visible element below $Root whose name matches $Name (regex), optionally of a control type (e.g. 'TreeItem')
function Find-Element([string]$Name, $Root = $script:Main, [string]$Type, [int]$TimeoutSec = 10) {
    Wait-Until {
        Get-AllElements $Root | Where-Object {
            $_.Current.Name -match $Name -and -not $_.Current.IsOffscreen -and (-not $Type -or $_.Current.ControlType -eq $ControlType::$Type)
        } | Select-Object -First 1
    } $TimeoutSec
}

# Elements of a control type below $Root, top to bottom
function Find-Elements([string]$Type, $Root = $script:Main) {
    Get-AllElements $Root | Where-Object { $_.Current.ControlType -eq $ControlType::$Type -and -not $_.Current.IsOffscreen } |
        Sort-Object { $_.Current.BoundingRectangle.Y }, { $_.Current.BoundingRectangle.X }
}

# Visible text boxes below $Root, top to bottom (WinForms text boxes show up as panes, so match the window class)
function Find-Edits($Root = $script:Main) {
    Get-AllElements $Root | Where-Object { $_.Current.ClassName -match 'EDIT' -and -not $_.Current.IsOffscreen -and $_.Current.BoundingRectangle.Width -gt 0 } |
        Sort-Object { $_.Current.BoundingRectangle.Y }, { $_.Current.BoundingRectangle.X }
}
#endregion

#region Feed editor
# The editor's tree (SysTreeView32) and its toolbar's "GnuPG key" combo box aren't exposed via UI Automation;
# find their native windows and drive them with window messages instead (no mouse, no keyboard, no focus needed).
function Get-ChildWindows([string]$Class, $Root = $script:Main) {
    [Gui]::ChildWindows([IntPtr]$Root.Current.NativeWindowHandle) | Where-Object { $_.Item2 -match $Class } |
        ForEach-Object { [pscustomobject]@{ Handle = $_.Item1; Class = $_.Item2; Rect = $_.Item3 } } |
        Sort-Object { $_.Rect.Y }, { $_.Rect.X }
}

function Get-TreeHandle { (Get-ChildWindows 'SysTreeView32' | Select-Object -First 1).Handle }

# Tree item (HTREEITEM) by child indexes from the root, e.g. @(8, 4): the root's 9th child's 5th child
function Get-TreeItem([int[]]$Path = @()) {
    $tree = Get-TreeHandle
    $item = [Gui]::SendMessageInt($tree, 0x110A, [IntPtr]0, [IntPtr]::Zero) # TVM_GETNEXTITEM, TVGN_ROOT
    foreach ($index in $Path) {
        $item = [Gui]::SendMessageInt($tree, 0x110A, [IntPtr]4, $item) # TVGN_CHILD
        for ($i = 0; $i -lt $index; $i++) { $item = [Gui]::SendMessageInt($tree, 0x110A, [IntPtr]1, $item) } # TVGN_NEXT
        if ($item -eq [IntPtr]::Zero) { throw "Tree item $($Path -join '/') not found (expand its parents first)" }
    }
    $item
}
function Expand-TreeItem([int[]]$Path = @()) {
    [Gui]::SendMessageInt((Get-TreeHandle), 0x1102, [IntPtr]2, (Get-TreeItem $Path)) | Out-Null # TVM_EXPAND, TVE_EXPAND
    Start-Sleep -Milliseconds 300
}
# Selects a tree item like a click would (the editor reacts to the TVN_SELCHANGED notification)
function Select-TreeItem([int[]]$Path = @()) {
    [Gui]::SendMessageInt((Get-TreeHandle), 0x110B, [IntPtr]9, (Get-TreeItem $Path)) | Out-Null # TVM_SELECTITEM, TVGN_CARET
    Start-Sleep -Milliseconds 600
}

# Screen point on the label of a visible tree row (0-based from the top, tree not scrolled) at a nesting depth (root = 0)
function Get-TreeRowPoint([int]$Row, [int]$Depth) {
    $tree = Get-ChildWindows 'SysTreeView32' | Select-Object -First 1
    [Gui]::SendMessageInt($tree.Handle, 0x110B, [IntPtr]5, (Get-TreeItem)) | Out-Null # TVM_SELECTITEM, TVGN_FIRSTVISIBLE: scroll to the top
    $height = [int][Gui]::SendMessageInt($tree.Handle, 0x111C, [IntPtr]::Zero, [IntPtr]::Zero) # TVM_GETITEMHEIGHT
    $indent = [int][Gui]::SendMessageInt($tree.Handle, 0x1106, [IntPtr]::Zero, [IntPtr]::Zero) # TVM_GETINDENT
    [pscustomobject]@{ X = $tree.Rect.X + ($Depth + 1) * $indent + 50; Y = $tree.Rect.Y + 2 + $Row * $height + [int]($height / 2) }
}

# Selects the demo key in the toolbar's "GnuPG key" combo box (entries: blank, demo key, "New key...")
function Select-DemoKey {
    $combo = (Get-ChildWindows 'COMBOBOX' | Select-Object -First 1).Handle # topmost: the toolbar's
    [Gui]::SendMessageInt($combo, 0x014E, [IntPtr]1, [IntPtr]::Zero) | Out-Null # CB_SETCURSEL
    # Notify like a user selection would: WM_COMMAND(CBN_SELCHANGE) to the parent, which WinForms reflects to the combo box
    $id = [Gui]::GetDlgCtrlID($combo)
    [Gui]::SendMessageInt([Gui]::GetParent($combo), 0x0111, [IntPtr]((1 -shl 16) -bor $id), $combo) | Out-Null
    Start-Sleep -Milliseconds 300
}

# Renders the window on stage to <OutDir>\<Name>.png (PrintWindow; see Helpers.ps1)
function Save-Still([string]$Name, $Window = $script:Main) {
    $path = Join-Path $OutDir "$Name.png"
    [Gui]::SaveWindow([IntPtr]$Window.Current.NativeWindowHandle, $path)
    Write-Host "Saved $path"
}
#endregion

#region Wizard
# Clicks a button by name (regex, e.g. '^Next'). Buttons with images (welcome window) show up as panes. Events: <Label>Move, <Label>
function Invoke-Button([string]$Name, [string]$Label, $Root = $script:Main, [int]$MoveMs = 600) {
    $button = Wait-Until {
        Get-AllElements $Root | Where-Object {
            $_.Current.Name -match $Name -and -not $_.Current.IsOffscreen -and $_.Current.ControlType -in $ControlType::Button, $ControlType::Pane
        } | Select-Object -First 1
    } 10
    if (-not $button.Current.IsEnabled) { throw "Button '$Name' is disabled" }
    $pos = Get-Center $button
    ClickAt $pos.X $pos.Y $Label $MoveMs
}

# Screen point of the first item matching $Text (regex) in the first visible list box below $Root
# (WinForms list boxes don't expose their items via UI Automation; Windows marshals LB_GETTEXT across processes)
function Get-ListBoxItemPoint([string]$Text, $Root = $script:Main) {
    $box = Get-ChildWindows 'LISTBOX' $Root | Where-Object { $_.Rect.Width -gt 0 } | Select-Object -First 1
    $count = [int][Gui]::SendMessageInt($box.Handle, 0x018B, [IntPtr]::Zero, [IntPtr]::Zero) # LB_GETCOUNT
    $top = [int][Gui]::SendMessageInt($box.Handle, 0x018E, [IntPtr]::Zero, [IntPtr]::Zero) # LB_GETTOPINDEX
    $height = [int][Gui]::SendMessageInt($box.Handle, 0x01A1, [IntPtr]::Zero, [IntPtr]::Zero) # LB_GETITEMHEIGHT
    for ($i = 0; $i -lt $count; $i++) {
        $sb = [System.Text.StringBuilder]::new(1024)
        [Gui]::SendMessageText($box.Handle, 0x0189, [IntPtr]$i, $sb) | Out-Null # LB_GETTEXT
        if ($sb.ToString() -match $Text) {
            return [pscustomobject]@{ X = $box.Rect.X + 60; Y = $box.Rect.Y + 2 + ($i - $top) * $height + [int]($height / 2) }
        }
    }
    throw "No list box item matches '$Text'"
}

# Clicks the value cell of a property grid row, replaces the value and presses Enter. Events: <Label>Move, <Label>, <Label>Typed
function Set-GridValue([string]$Row, [string]$Text, [string]$Label, [int]$DelayMs = 60) {
    $r = (Find-Element "^$Row$" -Type DataItem).Current.BoundingRectangle
    ClickAt ([int]($r.X + $r.Width * 0.75)) ([int]($r.Y + $r.Height / 2)) $Label
    Start-Sleep -Milliseconds 300
    Send-Key '{END}+{HOME}'
    Type-Text $Text $DelayMs $script:MainHandle
    Send-Key '{ENTER}'
    Log "${Label}Typed"
    Start-Sleep -Milliseconds 400
}

# Clicks a text box and types into it. Events: <Label>Move, <Label>, <Label>Typed
function Set-Field($Element, [string]$Text, [string]$Label, [int]$DelayMs = 60) {
    $pos = Get-Center $Element
    ClickAt $pos.X $pos.Y $Label
    Start-Sleep -Milliseconds 250
    Type-Text $Text $DelayMs ([IntPtr]$script:Main.Current.NativeWindowHandle)
    Log "${Label}Typed"
    Start-Sleep -Milliseconds 400
}

# Waits until an element whose name matches $Name appears below $Root (e.g. a wizard page's text), meanwhile logging
# progress windows of our process (DialogTaskHandler). Events: progressShown<Suffix>, progressClosed<Suffix> (only if a
# progress window showed), <Label>
function Wait-Page([string]$Name, [string]$Label, $Root = $script:Main, [string]$Suffix = '', [int]$TimeoutSec = 900) {
    $rootHandle = $Root.Current.NativeWindowHandle
    $script:progressSeen = $false
    Wait-Until {
        $progress = Get-ProcessWindows -Exclude $rootHandle, $script:WelcomeHandle
        if ($progress -and -not $script:progressSeen) { $script:progressSeen = $true; Log "progressShown$Suffix" }
        if (-not $progress -and $script:progressSeen) {
            $script:progressSeen = $false; Log "progressClosed$Suffix"
        }
        -not $progress -and (Get-AllElements $Root | Where-Object { $_.Current.Name -match $Name -and -not $_.Current.IsOffscreen } | Select-Object -First 1)
    } $TimeoutSec | Out-Null
    Log $Label
    Start-Sleep -Milliseconds 300
}

# Answers a message box of our process (e.g. "Skip the icon?") by clicking a button. Events: <Label>Shown, <Label>Move, <Label>
# Message boxes are task dialogs (#32770) with command links ("→ Yes"), so match the name only, not the control type.
function Confirm-MessageBox([string]$Button, [string]$Label, [int]$HoldMs = 1200) {
    $box = Wait-Until { Get-ProcessWindows -Exclude $script:MainHandle.ToInt32(), $script:WelcomeHandle | Where-Object { $_.Current.ClassName -eq '#32770' } | Select-Object -First 1 } 10
    Log "${Label}Shown"
    Start-Sleep -Milliseconds 500
    $element = Get-AllElements $box | Where-Object { $_.Current.Name -match $Button -and -not $_.Current.IsOffscreen } | Select-Object -Last 1 # innermost
    if (-not $element) { throw "No '$Button' in the message box:`n$((Show-Elements $box) -join "`n")" }
    Start-Sleep -Milliseconds $HoldMs
    $pos = Get-Center $element
    ClickAt $pos.X $pos.Y $Label 500
    Start-Sleep -Milliseconds 400
}
#endregion

#region Generic
# Sends a key (SendKeys syntax, e.g. '{ENTER}') if the window on stage is in the foreground
function Send-Key([string]$Keys) {
    if ([Gui]::GetForegroundWindow() -ne $script:MainHandle) { throw 'Refusing to type: target window is not in the foreground' }
    [System.Windows.Forms.SendKeys]::SendWait($Keys)
}

# Waits for a top-level window of our process whose title matches $Title; -Center moves it to $DialogCenter,
# -Rect to a fixed rectangle (cut the frames before the event: it is logged after the move). Events: <Label>. Returns the window.
function Wait-Window([string]$Title, [string]$Label, [switch]$Center, [hashtable]$Rect, [int]$TimeoutSec = 60) {
    $window = Wait-Until { Get-PublishWindow $Title } $TimeoutSec
    if ($Center -or $Rect) { Move-Stage ([IntPtr]$window.Current.NativeWindowHandle) $window $Rect -Center:$Center }
    Log $Label
    $window
}
#endregion
