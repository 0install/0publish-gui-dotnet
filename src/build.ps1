$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

$vsDir = . "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath -format value
$msBuild = "$vsDir\MSBuild\15.0\Bin\amd64\MSBuild.exe"

. $msBuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release
nuget pack ZeroInstall.Tools.nuspec -Properties Version=$(Get-Content ..\VERSION) -Symbols -OutputDirectory ..\build

popd
