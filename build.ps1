Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

src\build.ps1 $Version
.\0install.ps1 run --batch https://apps.0install.net/0install/0template.xml 0publish-win.xml.template version=$Version

popd
