# 0publish-gui - .NET version

[![Build](https://github.com/0install/0publish-gui-dotnet/workflows/Build/badge.svg)](https://github.com/0install/0publish-gui-dotnet/actions?query=workflow%3ABuild)  
Publishing a program using Zero Install requires you to create an XML file listing the available versions, where to get them, and what other software they depend on.
  
This program provides a simple graphical interface for creating and editing these feeds.

**[Documentation and download instructions](https://docs.0install.net/tools/0publish-gui/)**

## Other versions

See also the [Python version](https://github.com/0install/0publish-gui) of this program, for use on Linux and macOS.

## Building

The source code is in [`src/`](src/) and generated artifacts are placed in `artifacts/`.  
The source code does not contain version numbers. Instead the version is determined during CI using [GitVersion](https://gitversion.net/).

To build install [Visual Studio 2022 v17.8 or newer](https://www.visualstudio.com/downloads/) and run `.\build.ps1`.  

## Contributing

We welcome contributions to this project such as bug reports, recommendations, pull requests and [translations](https://www.transifex.com/eicher/0install-win/). If you have any questions feel free to pitch in on our [friendly mailing list](https://0install.net/support.html#lists).

This repository contains an [EditorConfig](http://editorconfig.org/) file. Please make sure to use an editor that supports it to ensure consistent code style, file encoding, etc.. For full tooling support for all style and naming conventions consider using JetBrains' [ReSharper](https://www.jetbrains.com/resharper/) or [Rider](https://www.jetbrains.com/rider/) products.
