$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

if (!(Get-Command 0install -ErrorAction SilentlyContinue)) {
    # Put 0install in PATH using Bootstrapper
    mkdir -Force "$env:TEMP\zero-install" | Out-Null
    Invoke-WebRequest "https://0install.de/files/zero-install.exe" -OutFile "$env:TEMP\zero-install\0install.exe"
    $env:PATH = "$env:TEMP\zero-install;$env:PATH"
}

rm -Force ..\build\Release\*.xml
rm -Force ..\build\Release\*.pdb

if ($Host.Name -ne 'ConsoleHost') {$ErrorActionPreference = "ContinueSilent"} # Avoid treating stderr output as failure condition
0install run --batch http://0install.net/tools/0template.xml ZeroInstall_Tools.xml.template version=$(Get-Content ..\VERSION)

popd
