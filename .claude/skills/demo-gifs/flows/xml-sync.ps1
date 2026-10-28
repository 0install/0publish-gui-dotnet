# Feed editor, graphical and XML view in sync (~15 s): select an implementation -> change its version in the property
# grid (the XML view follows) -> type an invalid character in the XML view (red underline, tooltip, status bar error)
# -> delete it again (valid, the tree follows). Works on the sample feed in $WorkDir and never saves it.

$Stage = @{
    Arguments = @($App.SampleFeed); Title = '^Zero Install Feed Editor'; Window = $EditorWindow
    Setup = { Expand-TreeItem @(8) } # the group (see editor-stills.ps1 for the tree layout)
}
$NewVersion = '3.0.25'

$Takes = [ordered]@{
    main = {
        Start-Sleep -Milliseconds 800
        # Select the newest 64-bit implementation: visible row 14 (root, 9 children, group's 5th child), depth 2
        $p = Get-TreeRowPoint 14 2
        ClickAt $p.X $p.Y 'clickImpl'
        Start-Sleep -Milliseconds 1200

        # Property grid: Version row -> value cell
        $row = Find-Element '^Version$' -Type DataItem
        $r = $row.Current.BoundingRectangle
        ClickAt ([int]($r.X + $r.Width * 0.75)) ([int]($r.Y + $r.Height / 2)) 'clickVersion'
        Start-Sleep -Milliseconds 300
        Send-Key ('{END}+{HOME}')
        Type-Text $NewVersion 120 $script:MainHandle
        Send-Key ('{ENTER}')
        Log 'versionChanged'
        Start-Sleep -Milliseconds 2000

        # XML view: put the caret behind the version number and type an invalid character
        $p = Get-XmlVersionPoint
        ClickAt $p.X $p.Y 'clickXml'
        Start-Sleep -Milliseconds 400
        Type-Text 'x' 150 $script:MainHandle
        Log 'typedInvalid'
        Start-Sleep -Milliseconds 1500
        [Gui]::MoveTo($p.X - 12, $p.Y + 2, 400) # hover over the underlined value for the tooltip
        Log 'hover'
        Start-Sleep -Milliseconds 2500
        Send-Key ('{BACKSPACE}')
        Log 'fixed'
        Start-Sleep -Milliseconds 2000
    }
}

# Position right behind the last digit of version="..." in line 1 of the XML view, relative to the editor window
# ($EditorWindow incl. its invisible 7 px side borders): 53 characters of 8 px from the text start at x 55, line 1 at y 471.
# Measured on a still of the selected implementation; re-measure if the layout, font or attribute order changes.
$XmlVersionPoint = @{ X = 55 + 53 * 8 - 1; Y = 471 }
function Get-XmlVersionPoint {
    $w = $script:Main.Current.BoundingRectangle
    [pscustomobject]@{ X = [int]($w.X + $XmlVersionPoint.X); Y = [int]($w.Y + $XmlVersionPoint.Y) }
}

function Get-Cuts {
    Clip main (T clickImplMove -0.2) (T versionChanged 1.5)                 # select implementation, change the version
    Clip main (T clickXmlMove -0.1) (T hover 2.3)                           # typo in the XML view, error, tooltip
    Clip main (T fixed -0.1) (T fixed 1.8)                                  # fixed again
}
