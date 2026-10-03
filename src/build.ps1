Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Find-MSBuild {
    if (Test-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe") {
        $vsDir = . "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -products * -property installationPath -format value -version 18.0
        if ($vsDir) { return "$vsDir\MSBuild\Current\Bin\amd64\MSBuild.exe" }
    }
}

function Run-MSBuild {
    $msbuild = Find-Msbuild
    if (!$msbuild) { throw "You need Visual Studio 2026 v18.0 or newer to build this project" }
    . $msbuild @args
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

echo "Build binaries"
if ($env:CI) { $ci = "/p:ContinuousIntegrationBuild=True" }
Run-MSBuild /v:Quiet /Restore /t:Build $ci /p:Configuration=Release /p:Version=$Version ZeroInstall.Publish.WinForms.slnx
Remove-Item ..\artifacts\Release\net48 -Recurse -Include *.xml,*.pdb,Microsoft.CodeAnalysis*

popd
