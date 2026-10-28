---
name: demo-gifs
description: Capture screenshots and record animated GIF demos of 0publish-gui (the Windows feed editor and New Feed Wizard) for docs.0install.net, by scripting the real mouse and keyboard, screen-recording with ffmpeg and cutting automatically from an event log. Use when asked to create, re-take or update screenshots, demo GIFs or screencasts of this GUI.
---

# Screenshots and demo GIFs of 0publish-gui

`Helpers.ps1` and `Build.ps1` are copies with small changes; `0install-win\.claude\skills\demo-gifs\reference.md` explains recording, cutting and troubleshooting in depth and applies here too.

A **flow** (`flows\<name>.ps1`) describes one demo:
- **`$Stage`:** how to start 0publish-gui for each take: `Arguments` (relative to the work dir), `Title` (window title regex), `Window` (rect) or `Center = $true`, optional `Setup` script block (unrecorded, no mouse).
- **`$Takes`:** scripted GUI actions; each take is recorded as one video with a fresh 0publish-gui.
- **`Get-Cuts`:** selects clips from the takes, relative to events logged while recording.
- **`$Stills`:** alternatively, screenshots: script blocks calling `Save-Still <name>`.

| Script | Does |
|---|---|
| `scripts\Prepare.ps1` | Creates the sandbox: demo GnuPG key and sample feed (below). Run once. |
| `scripts\Stills.ps1 -Flow <name>` | Captures stills with UI Automation and PrintWindow: no mouse, other windows may stay open. |
| `scripts\Record.ps1 -Flow <name>` | Records the takes (drives the real mouse and keyboard). |
| `scripts\Build.ps1 -Flow <name> -Review` | Cuts a GIF; never touches the screen. |

Output goes to `artifacts\demo-gifs\` (gitignored): `<name>.png`, `<flow>.gif`, `<flow>.mp4`, `<flow>-review.png`, `recordings\<name>\`.

## Sandbox: keep the user's keys and files out

- **`artifacts\demo-gifs\gnupg`:** an unprotected key "Demo Publisher <demo@example.com>" in the legacy `pubring.gpg`/`secring.gpg` files that 0publish-gui (BouncyCastle) reads. 0publish-gui is always started with `GNUPGHOME` pointing here, so the user's real keys never appear in key lists or screenshots and are never used.
- **`artifacts\demo-gifs\work`:** the only place feeds are opened from or saved to. `vlc.xml` is a trimmed, unsigned copy of `apps\feeds\gui\vlc.xml` with the 6 newest implementations.
- Takes and stills stop the 0publish-gui they started (only processes of `$PublishExe`), discarding unsaved changes.

## Ground rules: this drives the user's real desktop

- **Ask before every recording or stills run.** Say which flow and take, roughly how long it takes, and that the user must not touch the mouse or keyboard meanwhile. Wait for a go-ahead.
- **Never sign with, list or export the user's real keys.** Don't start 0publish-gui without the demo `GNUPGHOME`. Never click "New key" (launches `gpg --gen-key`).
- **Don't run the installer capture** (Installer Capture page → Next): it snapshots the system and runs an installer.
- **Network:** `new-feed-wizard` downloads ~40 MB (VLC); selecting an icon node downloads its preview; "Add missing" downloads the archive. Rehearse with a locally served copy (`-ArchiveUrl`, see below).
- **Check for personal data** before delivering: file dialogs, window titles, the desktop around the canvas.
- **Never weaken the safety guards.** If a script stops with "Refusing to click/type", find out why.

## Prerequisites

- Primary monitor 1920×1080 at 100% scaling; English UI.
- **No other windows in the canvas** (screen x 540–1380, y 200–880), including the window this conversation runs in. `Record.ps1` refuses to start otherwise. Not needed for `Stills.ps1`.
- `ffmpeg`, `magick` and `gpg` on the `PATH` (`0install alias ffmpeg https://apps.0install.net/utils/ffmpeg.xml`, likewise `imagemagick.xml`, `gnupg.xml`).
- 0publish-gui built: `src\build.ps1` (→ `artifacts\Release\net48\0publish-gui.exe`).

## Flows

| Flow | Kind | Shows | Docs image |
|---|---|---|---|
| `editor-stills` | stills `main`, `archive` | Editor overview with the demo key; an `<archive>` selected | `img/screens/0publish-win/main.png`, `archive.png` |
| `new-feed-wizard` | GIF | Welcome → wizard with the VLC archive → download/extract (sped up) → … → the feed in the editor | `wizard.gif` |
| `xml-sync` | GIF | Change a version in the property grid → XML follows; typo in the XML → underline, status bar error; fix | `xml-sync.gif` (was `xml-error.png`) |

```powershell
.\scripts\Prepare.ps1
.\scripts\Stills.ps1 -Flow editor-stills
# Rehearse the wizard without downloading: serve a copy of the archive locally
python -m http.server 8765 --bind 127.0.0.1 --directory <dir with vlc-3.0.24-win64.7z>
.\scripts\Record.ps1 -Flow new-feed-wizard -Recording test-wizard -ArchiveUrl http://127.0.0.1:8765/vlc-3.0.24-win64.7z
.\scripts\Record.ps1 -Flow new-feed-wizard
.\scripts\Build.ps1 -Flow new-feed-wizard -Review
```

## Writing flows

**Steps** (`scripts\Steps.ps1`; most log events):
- Stage/process: `Initialize-Stage`, `Start-Publish`, `Stop-Publish`, `Get-PublishWindow`.
- Lookup: `Find-Element <regex> [-Root] [-Type DataItem|Button|ComboBox|...]`, `Find-Elements <type>`, `Find-Edits`, `Get-ProcessWindows`, `Get-ChildWindows <class>`.
- Editor: `Expand-TreeItem <path>`, `Select-TreeItem <path>` (child indexes from the root, via window messages), `Get-TreeRowPoint <row> <depth>` (for clicks), `Select-DemoKey`, `Save-Still`.
- Wizard: `Invoke-Button`, `Set-Field`, `Set-GridValue <row>`, `Get-ListBoxItemPoint <regex>`, `Wait-Page` (logs `progressShown<Suffix>`/`progressClosed<Suffix>` for progress dialogs), `Confirm-MessageBox`, `Wait-Window [-Center|-Rect]`, `Send-Key`.
- From Helpers: `ClickAt`, `Type-Text`, `Log`, `Show-Elements` (discover control names).

**What UI Automation does and doesn't show here** (found the hard way):
- Not exposed: the editor's tree items, the toolbar's "GnuPG key" combo box (ToolStripComboBox, also refuses focus), list box items. Use the native windows (`Get-ChildWindows`) and window messages instead.
- Text boxes and image buttons (welcome window) show up as `Pane`, so match text boxes by class (`Find-Edits`) and buttons by name.
- Owned windows (the wizard, progress dialogs, message boxes) are nested below their owner instead of being top-level, and the wizard has no name. `Get-ProcessWindows` uses `EnumWindows`.
- Message boxes are task dialogs (`#32770`) with command links ("→ Yes").
- Property grid rows are `DataItem`s named after the property.
- Ticking "I have a local copy" via automation didn't enable "Next"; hence the localhost rehearsal above.

The **cut DSL** (`T`, `Test-Event`, `Clip`) and rules of thumb are the same as in 0install-win: start a clip 0.1–0.2 s before a `…Move` event; windows moved on stage are visible at their own position before the event, so cut to `event − 0.1` and resume at `event + 0.04`.
