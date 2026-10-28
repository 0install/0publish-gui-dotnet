# Shared settings for Prepare.ps1, Record.ps1, Stills.ps1 and Build.ps1. Flow files may override any of these.
# All coordinates are screen pixels on the primary monitor (1920x1080 at 100% scaling).

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..\..\..").Path

# 0publish-gui to record (build it with src\build.ps1 first)
$PublishExe = Join-Path $RepoRoot 'artifacts\Release\net48\0publish-gui.exe'

# Recordings go to <OutDir>\recordings\<name>\, finished GIFs and stills to <OutDir>\
$OutDir = Join-Path $RepoRoot 'artifacts\demo-gifs'

# Throwaway GnuPG home with a demo key (created by Prepare.ps1). 0publish-gui is always started with GNUPGHOME
# pointing here, so the user's real keys never show up in the key lists and are never used for signing.
$DemoGnuPGHome = Join-Path $OutDir 'gnupg'
$DemoKeyUser = 'Demo Publisher <demo@example.com>'

# Feeds are opened from and saved to this directory only (sample data created by Prepare.ps1)
$WorkDir = Join-Path $OutDir 'work'

# Default app used by the steps and samples
$App = @{
    SourceFeed = Join-Path $RepoRoot '..\apps\feeds\gui\vlc.xml'  # sample feed is a trimmed copy of this
    SampleFeed = 'vlc.xml'                                        # in $WorkDir
    KeepImplementations = 6                                       # newest implementations kept in the sample feed
    ArchiveUrl = 'https://get.videolan.org/vlc/3.0.24/win64/vlc-3.0.24-win64.7z'
    EntryPoint = 'vlc.exe'
    Summary = 'plays most multimedia files and streaming protocols' # typed in the wizard's Details page
    Version = '3.0.24'
    FeedUrl = 'https://example.com/vlc.xml'
}

# Window layout
$EditorWindow = @{ X = 550; Y = 210; W = 820; H = 660 }  # feed editor (MainForm)
$DialogCenter = @{ X = 960; Y = 540 }                    # where Wait-Window -Center and Initialize-Stage -Center put windows

# Recording
$Fps = 30                                                # capture frame rate

# Crops a clip can use. The GIF has the size of Canvas; other crops are scaled to it (keep the same aspect ratio).
$Crops = @{
    Canvas = @{ X = 540; Y = 200; W = 840; H = 680 }      # feed editor, centered welcome window, wizard and dialogs
}
$Capture = $Crops.Canvas                                 # region grabbed by ffmpeg; must contain every crop
