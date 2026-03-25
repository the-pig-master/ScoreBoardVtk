# ScoreBoardVtk

`ScoreBoardVtk` is a modern C# / .NET 8 rewrite of a legacy sports scoreboard controller.

The project is focused on working with a real physical scoreboard over a serial port and provides:

- a `WPF` operator application,
- a `CMD` utility for COM-port discovery and signal testing,
- a shared `Core` layer with match logic, protocol, settings, and runtime,
- unit tests for the critical logic.

## What the project does

The application is designed to control a sports scoreboard and operator workflow.

Supported scenarios:

- basketball mode,
- volleyball mode,
- serial communication with the scoreboard controller,
- score and foul / set management,
- main game clock,
- shot clock `24 / 14`,
- manual buzzer,
- running text,
- keyboard shortcuts for core operator actions,
- persistent configuration in JSON files.

## Solution structure

The solution contains the following projects:

- `ScoreBoardVtk.Core`
  - domain models, controller logic, protocol, transports, settings, runtime
- `ScoreBoardVtk.Wpf`
  - operator desktop UI
- `ScoreBoardVtk.Cmd`
  - console utility for COM-port diagnostics and buzzer testing
- `ScoreBoardVtk.Tests`
  - unit tests

Open solution:

- `C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.sln`

## Main functionality

### Basketball

- home / guest score control
- foul counters
- quarter / overtime control
- main countdown clock
- configurable regular-time preset
- configurable overtime preset
- shot clock start / stop
- `Set 24`, `Set 14`, `Run 24`, `Run 14`
- period-change warning if the quarter is changed before time expires
- reset of timers only
- manual set dialog for score, fouls, period, game clock, and shot clock

### Volleyball

- home / guest score control
- set progression
- set counters

### Operator functions

- running text
- manual buzzer
- keyboard shortcuts configured from the settings screen
- keyboard shortcut labels shown directly on the main control buttons

### Technical features

- legacy `AT+GD` packet generation
- `CRC16`
- `CP1251` support for the legacy device protocol
- `Hardware`, `Mock`, and `Development` startup profiles
- built-in `MOCK` transport for testing without real hardware
- elapsed-time based basketball timing logic for better timing accuracy

## Requirements

- Windows
- .NET 8 SDK
- Visual Studio 2022 or newer recommended

## Configuration files

The project uses 2 JSON files.

### `hostsettings.json`

Location for WPF:

- `ScoreBoardVtk.Wpf\hostsettings.json`

Purpose:

- selects the startup profile,
- configures runtime intervals,
- points to the game settings file.

Supported profiles:

- `Hardware`
  - only real serial ports are available
- `Mock`
  - only the built-in `MOCK` port is available
- `Development`
  - both real serial ports and `MOCK` are available

Default project value:

- `Development`

### `settings.json`

Location for WPF:

- `ScoreBoardVtk.Wpf\settings.json`

Purpose:

- selected COM port,
- game mode,
- timer presets,
- buzzer durations,
- keyboard shortcuts,
- running text settings,
- foul display mode.

Important:

- settings on the `Settings` tab are staged;
- changes are applied only after pressing `Apply`.

## Quick start

### Run from Visual Studio

1. Open `ScoreBoardVtk.sln`.
2. Set `ScoreBoardVtk.Wpf` as startup project.
3. Check `ScoreBoardVtk.Wpf\hostsettings.json`.
4. Run the application.

### Run from command line

Build:

```powershell
dotnet build C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.sln
```

Run WPF:

```powershell
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Wpf\ScoreBoardVtk.Wpf.csproj
```

Run CMD utility:

```powershell
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj
```

## How to use the WPF application

### Basic workflow

1. Open the `Settings` tab.
2. Select the required `COM` port.
3. Set the required game mode and timer presets.
4. Press `Apply`.
5. Open the port.
6. Go to the `Game` tab and control the scoreboard.

### Basketball workflow

Main controls:

- `Start / Stop`
  - starts or stops the main game clock
- `Signal`
  - manual buzzer while the button is held
- `Period / Set`
  - changes the quarter
  - if basketball time has not expired yet, a warning dialog is shown
- `Reset`
  - resets only timers
- `Set`
  - opens a dialog to manually set scoreboard values

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

Score controls:

- home score `+1 / -1`
- guest score `+1 / -1`
- home fouls `+1 / -1`
- guest fouls `+1 / -1`

### Manual Set dialog

The `Set` dialog allows manual operator input for:

- home score,
- guest score,
- home fouls / sets,
- guest fouls / sets,
- period,
- main game time,
- shot clock time.

The dialog also contains `Reset All`, which resets only the dialog values to defaults.
Changes are applied only after pressing `OK`.

### Running text

At the bottom of the `Game` tab:

- enable or disable running text,
- edit the text sent to the scoreboard.

## Keyboard shortcuts

Keyboard shortcuts are configured on the `Settings` tab.

Supported actions:

- game clock start / stop,
- shot clock start / stop,
- `Set 24`,
- `Set 14`,
- `Run 24`,
- `Run 14`,
- home score `+1 / -1`,
- guest score `+1 / -1`,
- home fouls `+1 / -1`,
- guest fouls `+1 / -1`.

Notes:

- shortcuts become active on the `Game` tab,
- shortcuts do not trigger while typing in text fields or combo boxes,
- assigned keys are shown in brackets directly on the corresponding buttons.

## CMD utility

The console project is intended for diagnostics and quick hardware checks.

Supported commands:

- `list`
  - prints all available COM ports
- `buzz <COMx>`
  - sends a `0.3` second buzzer signal to the specified port
- `scan-all`
  - sends the test buzzer to each COM port with a `1` second pause between ports
- `watch`
  - watches port connect/disconnect events

Examples:

```powershell
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- list
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- buzz COM3
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- buzz MOCK
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- scan-all
dotnet run --project C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj -- watch
```

Interactive mode:

- start the CMD app without arguments,
- use keyboard menu with arrows and `Enter`.

Important safety note:

- `buzz` and `scan-all` send the same legacy game packet with the manual signal flag enabled,
- do not use `scan-all` during a live game unless it is safe to touch the scoreboard state.

## Build and test

Build the solution:

```powershell
dotnet build C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.sln
```

Run tests:

```powershell
dotnet test C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.Tests\ScoreBoardVtk.Tests.csproj
```

## Notes about hardware and protocol

This project preserves the legacy scoreboard communication model and packet format, but final validation must still be done against the real scoreboard controller.

Important points:

- the original legacy system used low-level serial / RS-485 handling,
- the current project reproduces the scoreboard logic and payload generation,
- real hardware compatibility must be verified on the target device.

## Current status

The project currently provides:

- working `WPF` operator UI,
- working `CMD` diagnostics utility,
- `MOCK` mode for safe local testing,
- automated tests for controller logic and runtime behavior.

If you plan to use the application on real hardware, the recommended order is:

1. start in `Mock` or `Development` profile,
2. verify operator workflow,
3. find the correct port with `list`, `watch`, or `buzz`,
4. validate behavior on the real scoreboard.
