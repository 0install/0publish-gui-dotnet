#!/bin/sh
set -e
cd `dirname $0`

#Handle Windows-style paths in project files
export MONO_IOMAP=all

mono NuGet.exe restore -Verbosity quiet
xbuild /nologo /v:q
