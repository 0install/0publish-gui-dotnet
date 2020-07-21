# Zero Install Publishing Tools

[![Build status](https://img.shields.io/appveyor/ci/0install/0publish-win.svg)](https://ci.appveyor.com/project/0install/0publish-win)  
The Zero Install Publishing Tools provide a graphical editor and wizard for creating [Zero Install feeds](https://0install.github.io/docs/packaging/). They also contain an alternative version of the [0publish command-line tool](https://docs.0install.net/tools/0publish/) optimized for use on Windows. These tools are based on [Zero Install .NET](https://github.com/0install/0install-dotnet).

**[Documentation and download instructions](https://docs.0install.net/tools/0publish-win/)**

## Building

The source code is in [`src/`](src/) and generated build artifacts are placed in `artifacts/`.  
There is a template in [`feed/`](feed/) for generating a Zero Install feed from the artifacts. For official releases this is published at: https://apps.0install.net/0install/0publish-win.xml

You need [Visual Studio 2019](https://www.visualstudio.com/downloads/) to build this project.

Run `.\build.ps1` in PowerShell to build everything. This script takes a version number as an input argument. The source code itself only contains dummy version numbers. The actual version is picked by continuous integration using [GitVersion](http://gitversion.readthedocs.io/).

## Contributing

We welcome contributions to this project such as bug reports, recommendations, pull requests and [translations](https://www.transifex.com/eicher/0install-win/). If you have any questions feel free to pitch in on our [friendly mailing list](https://0install.net/support.html#lists).

This repository contains an [EditorConfig](http://editorconfig.org/) file. Please make sure to use an editor that supports it to ensure consistent code style, file encoding, etc.. For full tooling support for all style and naming conventions consider using JetBrain's [ReSharper](https://www.jetbrains.com/resharper/) or [Rider](https://www.jetbrains.com/rider/) products.
