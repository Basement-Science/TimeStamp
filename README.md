# TimeStamp
A cross-platform* .NET CLI utility that adds (colored) Timestamps to each line of Text. 

_*NOTE: unix-support is largely untested as of now. Requires you to install .Net runtime._

## Features:
- local or UTC timezone selectable.
- 3 color modes: none, legacy, ANSI (full RGB)
- Self-contained, framework-independent executable. 
- Can write to a log file (since `v1.02`).
- detailed help included. (NOTE: see **BUG** below)
- tab-autocomplete available for options (requires [`dotnet complete`](https://docs.microsoft.com/en-us/dotnet/core/tools/enable-tab-autocomplete), not for all shells.)

## Known bugs:
- Help is **NOT** displayed properly using `-h`, `/h`, `--help`, `-?`, `/?`. Use an invalid option instead for now, such as `-x`.
- if TimeStamp receives text from another program (via pipe | stdin) that uses ANSI-color itself, colors can get messed up in various ways. To minimize problems, TimeStamp should be run with the `-c 2` option in this case.

## Build:
Using Visual Studio 2019 is recommended (with c# / .Net 5 SDK installed). The project uses NuGet Packages.