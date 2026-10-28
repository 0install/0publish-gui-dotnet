# Stills of the feed editor for docs/tools/0publish-gui.md (Windows tab):
#   main    - overview: the interface selected with the group expanded, its properties, the XML view, the demo key selected
#   archive - the <archive> of the newest 64-bit implementation selected
# Run with: scripts\Stills.ps1 -Flow editor-stills
# Root's children: 3 categories, entry-point, capabilities, 2 icons, package-implementation, group (index 8).
# The group holds the sample's 6 implementations, newest last, x86_64 before i486 for each version (newest x86_64: index 4).

$Stage = @{ Arguments = @($App.SampleFeed); Title = '^Zero Install Feed Editor'; Window = $EditorWindow }

$Stills = [ordered]@{
    main = {
        Select-DemoKey
        Expand-TreeItem @(8)
        Select-TreeItem
        Save-Still 'main'
    }
    archive = {
        Expand-TreeItem @(8, 4)
        Select-TreeItem @(8, 4, 0)
        Save-Still 'archive'
    }
}
