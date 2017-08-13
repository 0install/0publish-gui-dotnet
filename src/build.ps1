$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

nuget restore
. "$(. "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath -format value)\Common7\IDE\devenv.com" ZeroInstall.Tools.sln /Build Release
nuget pack Publish\Publish.csproj -Properties Configuration=Release -Symbols -OutputDirectory ..\build

popd
