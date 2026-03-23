# SportsScoreboardModern

Modern C# migration of the legacy scoreboard controller from `C:\WORK\final`.

## Projects

- `SportsScoreboardModern.Core`
  - shared models, state machine, serial protocol, settings store, COM transport
- `SportsScoreboardModern.App`
  - WinForms UI
- `SportsScoreboardModern.Wpf`
  - WPF UI using MVVM

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
- Persisted settings in `settings.json`

## Open in Visual Studio

Open:

- `C:\WORK\Repositories\SportsScoreboardModern\SportsScoreboardModern.sln`

Target framework:

- `.NET 8`

## Important hardware note

The legacy Borland application used low-level COM handling for RS-485 transmission control.  
This migration preserves the scoreboard logic and payload format, but final validation still needs to be done against the real display controller.
