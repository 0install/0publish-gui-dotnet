Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Find-MSBuild {
    if (Test-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe") {
        $vsDir = . "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -products * -property installationPath -format value -version 17.0
        if ($vsDir) {
            if (Test-Path "$vsDir\MSBuild\Current") { return "$vsDir\MSBuild\Current\Bin\amd64\MSBuild.exe" } else { return "$vsDir\MSBuild\15.0\Bin\amd64\MSBuild.exe" }
        }
    }
}

function Run-MSBuild {
    $msbuild = Find-Msbuild
    if (!$msbuild) { throw "You need Visual Studio 2022 or newer to build this project" }
    . $msbuild @args
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

# Build
if ($env:CI) { $ci = "/p:ContinuousIntegrationBuild=True" }
Run-MSBuild /v:Quiet /t:Restore /t:Build $ci /p:Configuration=Release /p:Version=$Version

# Package
tar -czf ..\artifacts\0publish-win-$Version.tar.gz -C ..\artifacts\Release\net472\win --exclude *.pdb *

popd
