Zero Install Publishing Tools
=============================

The Zero Install Publishing Tools contain the Windows version of the `0publish` command-line tool and a graphical feed editor.

[![TeamCity Build status](https://0install.de/teamcity/app/rest/builds/buildType:(id:ZeroInstall_PublishingTools_Build)/statusIcon)](https://0install.de/teamcity/viewType.html?buildTypeId=ZeroInstall_PublishingTools_Build&guest=1)

**[Documentation and download instructions](http://0install.de/docs/publishing/tools/)**

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `build` contains the results of various compilation processes. It is created on first usage.
- `release` contains scripts for creating a Zero Install feed and archive for publishing a build.

Building
--------
- You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) and [Zero Install](http://0install.de/downloads/) to build this project. Also make sure the [nuget command-line tool](https://www.nuget.org/downloads) is in your `PATH`.
- The file `VERSION` contains the current version number of the project.
- Run `.\Set-Version.ps1 "X.Y.Z"` in PowerShall to change the version number. This ensures that the version also gets set in other locations (e.g. `GlobalAssemblyInfo.cs`).
- Run `.\build.ps1` in PowerShell to build everything.
