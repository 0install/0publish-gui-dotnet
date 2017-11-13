#!/bin/bash
set -e
cd `dirname $0`

printf $1 > VERSION
sed -b -i "s/AssemblyVersion\(\".*\"\)/AssemblyVersion\(\"$1\"\)/" src/GlobalAssemblyInfo.cs
