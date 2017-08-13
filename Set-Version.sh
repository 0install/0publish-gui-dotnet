#!/bin/bash
set -e
cd `dirname $0`

printf $1 > VERSION
sed -b -i "s/PROJECT_NUMBER = \".*\"/PROJECT_NUMBER = \"$1\"/" doc/Doxyfile
sed -b -i "s/AssemblyVersion\(\".*\"\)/AssemblyVersion\(\"$1\"\)/" src/GlobalAssemblyInfo.cs
