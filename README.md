Zero Install Publishing Tools
=============================

The Zero Install Publishing Tools contain the .NET version of the `0publish` command-line tool and a graphical feed editor.

**[Tool documentation](https://0install.de/docs/publishing/tools/)**

They also provide the **[ZeroInstall.Publish](https://www.nuget.org/packages/ZeroInstall.Publish/)** NuGet package which these tools are based on. This allows developers to create their own feed authoring tools.

**[API documentation](http://0install.de/api/tools/)**

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `doc` contains a Doxyfile project for generation the API documentation.
- `build` contains the results of various compilation processes. It is created on first usage.
- `samples` contains code snippets in different languages illustrating how to use the Zero Install Backend.
- `release` contains scripts for creating a Zero Install feed and archive for publishing a build.

Building
--------
- You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) and [Zero Install](http://0install.de/downloads/) to build this project.
- The file `VERSION` contains the current version number of the project.
- Run `.\Set-Version.ps1 "X.Y.Z"` in PowerShall to change the version number. This ensures that the version also gets set in other locations (e.g. `GlobalAssemblyInfo.cs`).
- Run `.\build.ps1` in PowerShell to build everything.
