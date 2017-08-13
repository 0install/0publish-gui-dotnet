#!/bin/bash
set -e
cd `dirname $0`

rm -f ../build/Release/*.xml ../build/Release/*.pdb

0install run http://0install.net/tools/0template.xml ZeroInstall_Tools.xml.template version=$(< ../VERSION)
