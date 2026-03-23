# ScoreBoardVtk

Modern C# migration of the legacy scoreboard controller from `C:\WORK\final`.

## Projects

- `ScoreBoardVtk.Core`
  - shared models, state machine, protocol, transports, configuration, runtime scheduler
- `ScoreBoardVtk.Wpf`
  - WPF UI using MVVM
- `ScoreBoardVtk.Cmd`
  - console utility for COM-port discovery and signal testing
- `ScoreBoardVtk.Tests`
  - unit tests for state machine, protocol, runtime, transports, and composition

## What is implemented

- Basketball mode:
  - score control
  - period control
  - foul tracking
  - game timer
  - manual buzzer
  - 24 / 14 second shot clock
- Volleyball mode:
  - score control
  - set progression
- Serial communication:
  - `AT+GD` game-state packets
  - `AT+ST` time sync packets
  - CRC16
  - legacy byte remapping copied from the original project
- Persisted match settings in `settings.json`
- Persisted host/runtime profile in `hostsettings.json`

## Profiles

`hostsettings.json` controls how the app boots:

- `Hardware`
  - only real serial ports are used
- `Mock`
  - only the built-in `MOCK` port is available
- `Development`
  - both hardware serial ports and the `MOCK` port are available

Default profile: `Development`

Example `hostsettings.json`:

```json
{
  "profile": "Development",
  "gameSettingsFileName": "settings.json",
  "runtime": {
    "mainClockIntervalMilliseconds": 100,
    "mainSignalIntervalMilliseconds": 1000,
    "shotClockSignalIntervalMilliseconds": 100,
    "displayRefreshIntervalMilliseconds": 1000,
    "publishIntervalMilliseconds": 50
  }
}
```

## Open in Visual Studio

Open:

- `C:\WORK\Repositories\ScoreBoardVtk\ScoreBoardVtk.sln`

Target framework:

- `.NET 8`

## Important hardware note

The legacy Borland application used low-level COM handling for RS-485 transmission control.  
This migration preserves the scoreboard logic and payload format, but final validation still needs to be done against the real display controller.
