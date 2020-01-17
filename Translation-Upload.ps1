﻿Param ([Parameter(Mandatory=$True)][String]$User, [Parameter(Mandatory=$True)][String]$Password)
$ErrorActionPreference = "Stop"

function put ($relativeUri, $filePath) {
    curl.exe -k -L --user "${User}:${Password}" -i -X PUT -F "file=@$filePath" "https://www.transifex.com/api/2/project/0install-win/$relativeUri"
}

function upload($slug, $pathBase) {
    put "resource/$slug/content/" "$pathBase.resx"
    put "resource/$slug/translation/de/" "$pathBase.de.resx"
}

upload publish-cli "$PSScriptRoot\src\Publish.Cli\Properties\Resources"
upload publish-win "$PSScriptRoot\src\Publish.WinForms\Properties\Resources"
