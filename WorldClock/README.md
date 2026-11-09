# WorldClock

WorldClock displays Valheim's current in-game time in a configurable on-screen label. Its rich-text template supports time, world day and fixed day-phase placeholders, plus per-part font name, size and color; it also supports adjustable position and a font demo.

The implementation reads the public `EnvMan.GetDayFraction()` and `EnvMan.GetDay()` APIs, formats the values locally, and draws through Unity's immediate-mode GUI. It does not patch game methods, read private game state, require a server-side install or depend on `ValheimMods.Common`.

The feature direction is inspired by the configurable clock widget in [Clock Mod by aedenthorn](https://github.com/aedenthorn/ValheimMods/tree/master/ClockMod); this plugin is implemented independently for this repository.

## Build, test and deploy

```powershell
.\WorldClock\scripts\build.ps1
dotnet run --project .\WorldClock\tests\WorldClock.Tests.csproj --configuration Release
.\WorldClock\scripts\build.ps1 -Deploy
```

See the [Thunderstore README](Thunderstore/README.md) for user configuration and installation.