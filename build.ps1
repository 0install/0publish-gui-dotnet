Param ($Version = "0.1.0-pre")
$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

src\build.ps1 $Version
release\build.ps1 $Version

popd
