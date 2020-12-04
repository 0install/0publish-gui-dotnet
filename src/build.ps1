Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Find-MSBuild {
    if (Test-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe") {
        $vsDir = . "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -products * -property installationPath -format value -version 16.5
        if ($vsDir) {
            if (Test-Path "$vsDir\MSBuild\Current") { return "$vsDir\MSBuild\Current\Bin\amd64\MSBuild.exe" } else { return "$vsDir\MSBuild\15.0\Bin\amd64\MSBuild.exe" }
        }
    }
}

function Run-MSBuild {
    $msbuild = Find-Msbuild
    if (!$msbuild) { throw "You need Visual Studio 2019 v16.5+ to build this project" }
    . $msbuild @args
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

function SearchAndReplace($Value, $FilePath, $PatternLeft, $PatternRight)
{
    (Get-Content $FilePath -Encoding UTF8) `
        -replace "$PatternLeft.*$PatternRight", ($PatternLeft.Replace('\', '') + $Value + $PatternRight.Replace('\', '')) |
        Set-Content $FilePath -Encoding UTF8
}

# Inject version number
Set-Content -Path "Publish.WinForms\VERSION" -Value $Version -Encoding UTF8
$AssemblyVersion = $Version.Split("-")[0]
SearchAndReplace $AssemblyVersion GlobalAssemblyInfo.cs -PatternLeft 'AssemblyVersion\("' -PatternRight '"\)'

# Compile source code
Run-MSBuild /v:Quiet /t:Restore /t:Build /p:Configuration=Release

# Package
tar -czf ..\artifacts\0publish-win-$Version.tar.gz -C ..\artifacts\Release --exclude *.pdb *

popd
