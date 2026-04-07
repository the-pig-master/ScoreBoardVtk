# ScoreBoardVtk

`ScoreBoardVtk` is a modern `C# / .NET 8` operator application for a legacy sports scoreboard controller.

The project is intended to work with a real physical scoreboard over a serial port and includes:

- a `WPF` operator UI,
- a `CMD` utility for COM-port diagnostics and buzzer testing,
- a shared `Core` layer for match logic, protocol, settings, and runtime,
- unit tests for critical logic.

Russian documentation: [README.ru.md](C:/WORK/Repositories/ScoreBoardVtk/README.ru.md)

## Solution structure

- `ScoreBoardVtk.Core`
  - models, controller logic, protocol, transports, settings, runtime
- `ScoreBoardVtk.Wpf`
  - desktop operator UI
- `ScoreBoardVtk.Cmd`
  - console diagnostics utility
- `ScoreBoardVtk.Tests`
  - unit tests

Open solution:

- `C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.sln`

## Main functionality

### Basketball

- home / guest score control
- foul counters
- quarter / overtime control
- main game clock
- configurable regular-time preset
- configurable overtime preset
- shot clock `24 / 14`
- `Set 24`, `Set 14`, `Run 24`, `Run 14`
- timer-only reset
- manual set dialog for score, fouls, period, game clock, and shot clock
- period-change warning if time has not expired

### Volleyball

- home / guest score control
- set progression
- set counters

### Operator tools

- manual buzzer
- running text
- configurable keyboard shortcuts
- English and Russian UI localization
- optional `Debug` tab for manual payload sending

### Technical features

- legacy `AT+GD` packet generation
- `CRC16`
- `CP1251` support for the legacy protocol
- `Hardware`, `Mock`, and `Development` startup profiles
- built-in `MOCK` transport
- elapsed-time based basketball timing logic

## Configuration files

The application uses two JSON files.

### `hostsettings.json`

WPF location:

- [hostsettings.json](C:/WORK/Repositories/ScoreBoardVtk/ScoreBoardVtk.Wpf/hostsettings.json)

Purpose:

- selects the startup profile
- controls runtime timing and publish intervals
- points to the game settings file
- controls whether the `Debug` tab is visible
- provides the default payload encoding for the `CMD` utility

Profiles:

- `Hardware`
  - only real serial ports are available
- `Mock`
  - only the built-in `MOCK` port is available
- `Development`
  - real serial ports and `MOCK` are available

Useful fields:

- `showDebugTab`
  - `true` = show the `Debug` tab
  - `false` = hide it
- `payloadEncoding`
  - default single-byte encoding for `CMD` custom payload sending
  - default value: `windows-1251`

Release defaults:

- [hostsettings.Release.json](C:/WORK/Repositories/ScoreBoardVtk/ScoreBoardVtk.Wpf/hostsettings.Release.json)
  - `profile = Hardware`
  - `showDebugTab = false`

### `settings.json`

WPF location:

- [settings.json](C:/WORK/Repositories/ScoreBoardVtk/ScoreBoardVtk.Wpf/settings.json)

Purpose:

- selected COM port
- UI language
- game mode
- timer presets
- buzzer durations
- keyboard shortcuts
- running text settings
- foul display mode
- legacy packet behavior settings

Important fields:

- `mainSignalDurationSeconds`
  - duration of the main period-end buzzer
- `shotClockSignalDurationTenths`
  - duration of the `24`-second buzzer
- `useMainSignalFieldForShotClockSignal`
  - `true` = route the `24`-second signal through the same payload field as the main signal
  - `false` = use the dedicated `24`-second signal field in the payload

Important behavior:

- settings on the `Settings` tab are staged
- changes are applied only after pressing `Apply`

## WPF workflow

### Basic flow

1. Open the `Settings` tab.
2. Select the required `COM` port.
3. Set game mode and timer presets.
4. Press `Apply`.
5. Open the port.
6. Switch to the `Game` tab and control the scoreboard.

### Basketball controls

Main controls:

- `Start / Stop`
  - starts or stops the main game clock
- `Signal`
  - manual buzzer while held
- `Period / Set`
  - changes the quarter
- `Reset`
  - resets timers only
- `Set`
  - opens a dialog to set scoreboard values manually

Shot clock controls:

- `Start / Stop`
  - starts or stops the current shot clock value
- `Run 24`
  - sets `24` and starts shot clock
- `Run 14`
  - sets `14` and starts shot clock
- `Set 24`
  - sets `24` without starting
- `Set 14`
  - sets `14` without starting

### Manual Set dialog

The `Set` dialog allows manual input for:

- home score
- guest score
- home fouls / sets
- guest fouls / sets
- period
- main game time
- shot clock time

`Reset All` resets only the values inside the dialog.
Changes are applied only after pressing `OK`.

### Running text

At the bottom of the `Game` tab you can:

- enable or disable running text
- edit the message sent to the scoreboard

### Debug tab

The `Debug` tab is optional and is controlled by `hostsettings.json`.

It provides:

- `Debug mode`
  - pauses periodic automatic payload publishing
- manual payload text box
- `Send`
  - sends the entered payload to the scoreboard

When `Debug mode` is disabled, periodic automatic publishing resumes.

## Keyboard shortcuts

Keyboard shortcuts are configured on the `Settings` tab.

Supported actions:

- game clock start / stop
- shot clock start / stop
- `Set 24`
- `Set 14`
- `Run 24`
- `Run 14`
- home score `+1 / -1`
- guest score `+1 / -1`
- home fouls `+1 / -1`
- guest fouls `+1 / -1`

Notes:

- shortcuts are active on the `Game` tab
- shortcuts do not trigger while typing in text fields or combo boxes
- assigned keys are shown in brackets on the corresponding buttons

## CMD utility

The console project is intended for diagnostics and quick hardware checks.

Commands:

- `list`
  - prints all available COM ports
- `buzz <COMx>`
  - sends a `0.3` second buzzer signal to the specified port
- `payload <COMx> <payload> [--encoding <name|codepage>]`
  - sends a custom `AT+GD` payload to the specified port
- `encodings`
  - prints the list of supported single-byte encodings for legacy payloads
- `encoding <name|codepage>`
  - sets the payload encoding for the current interactive session
- `scan-all`
  - sends the test buzzer to each COM port with a `1` second pause
- `watch`
  - watches port connect/disconnect events

Examples:

```powershell
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- list
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- buzz COM3
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- payload COM3 "TEST"
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- payload COM3 "Привет" --encoding 866
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- encodings
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- buzz MOCK
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- scan-all
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- watch
```

Interactive mode:

- start the CMD app without arguments
- use the keyboard menu with arrows and `Enter`
- select the payload encoding in the menu
- send a custom payload from the menu without command-line arguments

## Build, test, and package

Build:

```powershell
dotnet build C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.sln
```

Run tests:

```powershell
dotnet test C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Tests\ScoreBoardVtk.Tests.csproj
```

Create `Release` archives and installers:

```powershell
powershell -ExecutionPolicy Bypass -File C:\WORK\Repositories\ScoreBoardVtk\build\package.ps1
```

Packaging output:

- `.zip` packages for `win-x86` and `win-x64`
- `setup.exe` installers for `win-x86` and `win-x64`

Requirements for installers:

- `Inno Setup 6` must be installed so that `ISCC.exe` is available

Packaging details:

- publish is `Release`
- publish is `self-contained`
- both `WPF` and `CMD` utility are included
- `CMD` is staged under `tools\cmd`

## Hardware notes

This project preserves the legacy scoreboard communication model and packet format, but final validation must still be done against the real scoreboard controller.

Important points:

- the original system used low-level serial / `RS-485` communication
- the current project reproduces the scoreboard logic and payload generation
- real hardware compatibility must be verified on the target device

## Current status

The project currently provides:

- working `WPF` operator UI
- working `CMD` diagnostics utility
- `MOCK` mode for safe local testing
- automated tests for controller logic, runtime behavior, and legacy payload generation

Recommended rollout order for real hardware:

1. start in `Mock` or `Development`
2. verify operator workflow
3. find the correct port with `list`, `watch`, or `buzz`
4. validate behavior on the real scoreboard
