$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

$Version = Get-Content ..\VERSION

# Do not include .NET XML documentation in release
rm -Force ..\build\Release\*.xml -Exclude *.VisualElementsManifest.xml

# Do not include debug symbols in release
rm -Force ..\build\Release\*.pdb

# Build feed and archive
cmd /c "0install run --batch http://0install.net/tools/0template.xml ZeroInstall_Tools.xml.template version=$Version 2>&1" # Redirect stderr to stdout
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

# Patch URL to point to GitHub release for tagged builds
if ($env:APPVEYOR_REPO_TAG -eq "true") {
    $path = Resolve-Path "ZeroInstall_Tools-$Version.xml"
    [xml]$xml = Get-Content $path
    $xml.interface.group.implementation.archive.href = "https://github.com/0install/0publish-win/releases/download/$Version/$($xml.interface.group.implementation.archive.href)"
    $xml.Save($path)
}

popd
