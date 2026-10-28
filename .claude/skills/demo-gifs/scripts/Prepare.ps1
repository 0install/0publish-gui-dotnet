<#
.SYNOPSIS
Creates the sandbox the flows run in (all below <OutDir>, never touching the user's profile):
- <OutDir>\gnupg: a GnuPG home with an unprotected demo key ($DemoKeyUser), in the legacy pubring.gpg/secring.gpg
  format that 0publish-gui (BouncyCastle) reads
- <OutDir>\work\vlc.xml: a trimmed, unsigned copy of the VLC feed from the apps repository
Safe to re-run; an existing demo key is kept. Needs gpg (GnuPG 1.4 or 2.x) on the PATH.
#>
param([switch]$Force)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Config.ps1"

#region Demo key
if ($Force -or -not (Test-Path "$DemoGnuPGHome\secring.gpg") -or (Get-Item "$DemoGnuPGHome\secring.gpg").Length -eq 0) {
    if (Test-Path $DemoGnuPGHome) { Remove-Item -Recurse -Force $DemoGnuPGHome }
    New-Item -ItemType Directory -Force $DemoGnuPGHome | Out-Null
    $env:GNUPGHOME = $DemoGnuPGHome
    $ErrorActionPreference = 'Continue' # gpg reports progress on stderr; check exit codes instead
    $params = "$DemoGnuPGHome\keyparams.txt"
    $DemoKeyUser -match '^(.+) <(.+)>$' | Out-Null
    @"
Key-Type: RSA
Key-Length: 2048
Key-Usage: sign
Name-Real: $($Matches[1])
Name-Email: $($Matches[2])
Expire-Date: 0
%no-protection
%commit
"@ | Set-Content -Encoding ascii $params
    gpg --batch --gen-key $params
    if ($LASTEXITCODE -ne 0) { throw 'gpg --gen-key failed' }
    # GnuPG 2.1+ keeps keys in pubring.kbx/private-keys-v1.d; export them to the legacy files BouncyCastle reads
    if (-not (Test-Path "$DemoGnuPGHome\secring.gpg") -or (Get-Item "$DemoGnuPGHome\secring.gpg").Length -eq 0) {
        gpg --batch --yes --output "$DemoGnuPGHome\pubring.gpg" --export
        gpg --batch --yes --pinentry-mode loopback --passphrase '' --output "$DemoGnuPGHome\secring.gpg" --export-secret-keys
        gpgconf --kill gpg-agent
    }
    Remove-Item Env:GNUPGHOME
    $ErrorActionPreference = 'Stop'
}
#endregion

#region Sample feed
New-Item -ItemType Directory -Force $WorkDir | Out-Null
$xml = [xml]::new()
$xml.PreserveWhitespace = $false
$xml.Load((Resolve-Path $App.SourceFeed).Path)
# Drop the signature block and all but the newest implementations, so the tree stays readable
foreach ($comment in @($xml.SelectNodes('//comment()'))) { $comment.ParentNode.RemoveChild($comment) | Out-Null }
$impls = @($xml.SelectNodes('//*[local-name()="implementation"]'))
$impls | Select-Object -First ($impls.Count - $App.KeepImplementations) | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
$settings = [System.Xml.XmlWriterSettings]@{ Indent = $true; Encoding = [System.Text.UTF8Encoding]::new($false) }
$writer = [System.Xml.XmlWriter]::Create((Join-Path $WorkDir $App.SampleFeed), $settings)
try { $xml.Save($writer) } finally { $writer.Dispose() }
#endregion

Get-ChildItem $DemoGnuPGHome, $WorkDir | Format-Table Directory, Name, Length
