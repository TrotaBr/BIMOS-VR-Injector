# Building the mod

## Prerequisites

- .NET SDK 6.0 or newer (`dotnet --version`)
- A MelonLoader-patched game that has been run at least once, so `Mods/`, `UserLibs/` and `UserData/` exist

## Reference assemblies

Game and loader assemblies are not committed. Create `libs/mono/` and copy in:

| File | From |
|---|---|
| `MelonLoader.dll` | `<Game>/MelonLoader/net35/` |
| `0Harmony.dll` | `<Game>/MelonLoader/net35/` |
| `UnityEngine*.dll` | `<Game>/<Game>_Data/Managed/` |

Take the whole `UnityEngine*` set, modern Unity splits the engine across dozens of module assemblies and the project globs them.

UniverseLib and Newtonsoft.Json come from NuGet automatically.

## Target framework

The Mono configuration targets `net45`, which suits most modern Mono games. To confirm for a specific game, look in its `Managed/` folder:

| Observation | Target |
|---|---|
| `mscorlib.dll` is 4.x, no `netstandard.dll` | `net45` |
| `mscorlib.dll` is 2.0, and `UnityEngine.UI.dll` is a 2.0 image | `net35` |

Do not rely on MelonLoader's `Runtime Type:` log line; it reports `net35` for every Mono game regardless.

`netstandard2.0` is not a safe target for games that do not ship `netstandard.dll`, the assembly will fail to load with a `TypeLoadException`.

To switch, change `<TargetFramework>` in the Mono `PropertyGroup` of `src/BimosVrInjector.Mod/BimosVrInjector.Mod.csproj`, and put `net35` first in `TargetFrameworks` in the Core project.

## Build

```bash
dotnet build src/BimosVrInjector.Mod -c Mono
```

Output is staged to `build/mono/`:

```
build/mono/Mods/BimosVrInjector.dll
build/mono/UserLibs/BimosVrInjector.Core.dll
build/mono/UserLibs/Newtonsoft.Json.dll
build/mono/UserLibs/UniverseLib.Mono.dll
```

Copy `Mods/` and `UserLibs/` into the game folder, preserving the split. `UserLibs/` is MelonLoader's location for shared libraries that mods depend on but which are not themselves mods.

## Tests

```bash
dotnet run --project tests/BimosVrInjector.Core.Tests
```

A plain console harness, no test framework dependency. It exercises the authoring log, JSON round-tripping, playback against a fresh fake scene, fuzzy resolution of a moved object, and the warn-and-skip path for a missing object. Ends with a pass/fail count.

Because Core has no engine dependencies, this runs anywhere and catches most regressions without launching a game.

## IL2CPP

An `Il2Cpp` configuration exists and compiles the same source with `IL2CPP` defined, targeting `net6.0`. It requires the interop assemblies from a MelonLoader IL2CPP install in `libs/il2cpp/`.

This path is **scaffolded but unverified**. Beyond the build, IL2CPP needs custom MonoBehaviours registered with the runtime via `ClassInjector`, and bundle-loaded prefabs carrying injected components are a known weak spot in the interop layer.
