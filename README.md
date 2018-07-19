Zero Install Publishing Tools
=============================

The Zero Install Publishing Tools contain the Windows version of the `0publish` command-line tool and a graphical feed editor.  
The Zero Install Publishing Tools are built upon **[Zero Install .NET](https://github.com/0install/0install-dotnet)**.

[![Build status](https://img.shields.io/appveyor/ci/0install/0publish-win.svg)](https://ci.appveyor.com/project/0install/0publish-win)

**[Documentation and download instructions](http://0install.de/docs/publishing/tools/)**

Directory structure
-------------------
- `src` contains source code.
- `artifacts` contains the results of various compilation processes. It is created on first usage.
- `release` contains scripts for creating a Zero Install feed and archive for publishing a build.

Building
--------
You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) to build this project.  
Run `.\build.ps1` in PowerShell to build everything. This script takes a version number as an input argument. The source code itself only contains dummy version numbers. The actual version is picked by continuous integration using [GitVersion](http://gitversion.readthedocs.io/).
