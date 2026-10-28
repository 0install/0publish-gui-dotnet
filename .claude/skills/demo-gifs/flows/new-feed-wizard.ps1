# New Feed Wizard (~25 s): welcome window -> wizard -> archive URL -> download + extract (sped up) -> archive dir ->
# entry point -> details -> icon -> security (demo key, feed URL) -> done -> the feed opens in the editor.
# Downloads $App.ArchiveUrl (~40 MB). To rehearse without downloading, serve a copy locally and pass its URL:
#   python -m http.server 8765 --bind 127.0.0.1 --directory <dir with the archive>
#   Record.ps1 -Flow new-feed-wizard -Recording test-wizard -ArchiveUrl http://127.0.0.1:8765/vlc-3.0.24-win64.7z
# The wizard writes nothing outside %TEMP% and signs nothing; the editor is stopped (unsaved) after the take.

$Stage = @{ Arguments = @(); Title = '^0publish-gui$'; Center = $true }

$Takes = [ordered]@{
    main = {
        $script:WelcomeHandle = $script:Main.Current.NativeWindowHandle
        Start-Sleep -Milliseconds 800
        Invoke-Button '^New Feed \(Wizard\)' 'clickWizard'
        # The wizard (AeroWizard) has no UIA name: take our process's other top-level window
        $wizard = Wait-Until { Get-ProcessWindows -Exclude $script:WelcomeHandle | Select-Object -First 1 } 30
        Start-Sleep -Milliseconds 300
        Move-Stage ([IntPtr]$wizard.Current.NativeWindowHandle) $wizard -Center
        Log 'wizardShown'
        $script:Main = $wizard
        $script:MainHandle = [IntPtr]$wizard.Current.NativeWindowHandle

        # Download
        $edits = @(Find-Edits $wizard)
        Set-Field $edits[0] $App.ArchiveUrl 'url' 25
        Invoke-Button '^Next' 'clickNext1'
        Wait-Page 'top-level directory' 'archivePage' -Suffix 'Download'

        # Archive: keep the default
        Start-Sleep -Milliseconds 1200
        Invoke-Button '^Next' 'clickNext2'
        Wait-Page 'starts the main application' 'entryPage' -Suffix 'Scan'

        # Entry point
        $pos = Get-ListBoxItemPoint "^$([regex]::Escape($App.EntryPoint)) "
        ClickAt $pos.X $pos.Y 'clickEntry'
        Start-Sleep -Milliseconds 600
        Invoke-Button '^Next' 'clickNext3'
        Wait-Page 'fill in the missing details' 'detailsPage' -Suffix 'Digest'

        # Details: name prefilled from the executable's metadata; set a real summary and the (required) version
        Start-Sleep -Milliseconds 1200
        Set-GridValue 'Summary' $App.Summary 'summary'
        Set-GridValue 'Version' $App.Version 'version'
        Start-Sleep -Milliseconds 800
        Invoke-Button '^Next' 'clickNext4'
        Wait-Page 'Icons need to be extracted' 'iconPage'

        # Icon: shows the extracted icon; skip uploading it
        Start-Sleep -Milliseconds 1500
        Invoke-Button '^Next' 'clickNext5'
        Confirm-MessageBox '^Yes' 'skipIcon'
        Wait-Page 'GnuPG signatures' 'securityPage'

        # Security: demo key and feed URL
        # Open the key list, click the second entry (blank, demo key)
        $combo = Get-ChildWindows 'COMBOBOX' $wizard | Where-Object { $_.Rect.Width -gt 0 } | Select-Object -First 1
        $itemHeight = [int][Gui]::SendMessageInt($combo.Handle, 0x0154, [IntPtr]0, [IntPtr]::Zero) # CB_GETITEMHEIGHT (list items)
        ClickAt ($combo.Rect.X + $combo.Rect.Width / 2) ($combo.Rect.Y + $combo.Rect.Height / 2) 'clickKeys'
        Start-Sleep -Milliseconds 800
        ClickAt ($combo.Rect.X + 120) ($combo.Rect.Bottom + 1 + $itemHeight + [int]($itemHeight / 2)) 'clickKey' 400
        Start-Sleep -Milliseconds 600
        if (-not [Gui]::SendMessageInt($combo.Handle, 0x0147, [IntPtr]::Zero, [IntPtr]::Zero).ToInt32()) { throw 'Demo key not selected' } # CB_GETCURSEL
        $uri = @(Find-Edits $wizard) | Select-Object -Last 1
        Set-Field $uri $App.FeedUrl 'feedUrl' 40
        Invoke-Button '^Next' 'clickNext6'
        Start-Sleep -Milliseconds 500
        if (Get-ProcessWindows -Exclude $script:MainHandle.ToInt32(), $script:WelcomeHandle) { throw 'Unexpected dialog after the Security page' }
        Wait-Page 'The wizard is done' 'donePage'

        # Done -> editor
        Start-Sleep -Milliseconds 1500
        Invoke-Button '^Finish' 'clickFinish'
        $script:Main = Wait-Window '^Zero Install Feed Editor' 'editorShown' -Rect $EditorWindow
        Start-Sleep -Seconds 3
    }
}

# Clip for a progress phase that may be too short to matter (e.g. a cached or local download)
function Clip-Progress([double]$Start, [double]$End, [double]$Speed) {
    if ($End - $Start -gt 0.1) { Clip main $Start $End -Speed $Speed }
}

function Get-Cuts {
    $Pace = 1.5 # everything but progress phases, to keep the GIF short
    Clip main (T clickWizardMove -0.2) (T wizardShown -0.1) -Speed $Pace                 # welcome window, click the wizard button
    Clip main (T wizardShown 0.04) (T clickNext1 0.2) -Speed $Pace                       # type the URL, Next
    Clip-Progress (T clickNext1 0.2) (T archivePage) 10                     # download + extract, sped up
    Clip main (T archivePage) (T clickNext2 0.2) -Speed $Pace                            # archive page
    Clip-Progress (T clickNext2 0.2) (T entryPage) 4                        # scanning for executables
    Clip main (T entryPage) (T clickNext3 0.2) -Speed $Pace                              # pick the entry point
    Clip-Progress (T clickNext3 0.2) (T detailsPage) 8                      # manifest digest
    Clip main ([Math]::Max((T clickNext3 0.2), (T detailsPage))) (T skipIcon 0.3) -Speed $Pace # details, icon, "skip icon?" -> Yes
    Clip main (T securityPage -0.1) (T clickFinish 0.2) -Speed $Pace                     # security, done
    Clip main (T editorShown 0.04) (T editorShown 2.5)                                   # the feed in the editor
}
