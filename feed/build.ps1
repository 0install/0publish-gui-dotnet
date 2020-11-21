Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

# Exclude .NET XML Documentation and Debug Symbols from release
rm -Force ..\artifacts\Release\*.xml,..\artifacts\Release\*.pdb

# Inspect version number
$stability = if($Version.Contains("-")) {"developer"} else {"stable"}

# Build feed and archive
..\0install.ps1 run --batch http://0install.net/tools/0template.xml 0publish-win.xml.template version=$Version stability=$stability
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

# Patch archive URL for release builds
if ($stability -eq "stable") {
    $path = Resolve-Path "0publish-win-$Version.xml"
    [xml]$xml = Get-Content $path
    $xml.interface.group.implementation.archive.href = "https://github.com/0install/0publish-win/releases/download/$Version/$($xml.interface.group.implementation.archive.href)"
    $xml.Save($path)
}

popd
