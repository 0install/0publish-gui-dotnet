$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

rm -Force ..\build\Release\*.xml
rm -Force ..\build\Release\*.pdb

0install run http://0install.net/tools/0template.xml ZeroInstall_Tools.xml.template version=$(Get-Content ..\VERSION)

popd
