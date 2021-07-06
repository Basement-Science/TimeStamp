# TimeStamp
A cross-platform .NET 6 CLI utility that adds (colored) Timestamps to each line of Text. 

_NOTE: Only Win-x64 builds are reasonably well tested._

## Features:
- local or UTC timezone selectable.
- 3 color modes: none, legacy, ANSI (full RGB)
- no installation, no dependencies. 
- can write to a log file.
- detailed help and example included. 
- tab-autocomplete available for options (requires [`dotnet complete`](https://docs.microsoft.com/en-us/dotnet/core/tools/enable-tab-autocomplete), not for all shells.)

## Known issues:
- if TimeStamp receives text from another program (via pipe | stdin) that uses ANSI-color itself, colors can get messed up in various ways. To minimize problems, TimeStamp should be run with the `-c 2` option in this case.

## Build:
Using Visual Studio 2019 is recommended (with c# / .Net 5+ SDK installed). The project uses NuGet Packages. To generate single-file executables, use [`.net 6.0 sdk`](https://dotnet.microsoft.com/download/dotnet) to build. _(tested with `dotnet 6.0-pre-5`)_ 

**Example:**

`dotnet publish --self-contained true --configuration Release --framework net6.0 --output .\net6.0-linux-x64 --runtime linux-x64`