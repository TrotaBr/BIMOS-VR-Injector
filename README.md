# BIMOS VR Injector

A [MelonLoader](https://github.com/LavaGang/MelonLoader) mod that converts flatscreen Unity games into VR using the [BIMOS](https://github.com/KadenZombie8/BIMOS) physics-based VR player rig.



## How it works

1. **Author mode** - browse the live scene hierarchy, disable or delete objects, place the VR rig, tag grabbables.
2. **Config** - every action is written to `UserData/BimosVrInjector/configs/<scene>.json`.
3. **Playback** - on each scene load the config is reapplied. Unity resets scenes on load, so nothing is assumed to persist.


## Features

- In-game hierarchy browser and object picker built on [UniverseLib](https://github.com/sinai-dev/UniverseLib)
- Scene-keyed JSON configs with fuzzy object resolution
- OpenXR bootstrap for games that were built without VR support
- Automatic physics-rate matching.
- Grab tagging, individually or in bulk.
- Works with BIMOS 1.0

## Requirements

| | |
|---|---|
| Game | Unity, **Mono** backend (IL2CPP is WIP) |
| Loader | MelonLoader 0.6.x |
| Headset | Any OpenXR runtime (SteamVR, Oculus/Meta Link, WMR) |
| Building | .NET SDK 6.0 or newer |
| Unity Editor | Unity Editor matching the target game's version (For the release, 6000.3.18f1 is required.)|

## Install

**Warning: The build on releases only works with Unity 6000.3.18+ games.**

**1.** Install [MelonLoader](https://github.com/LavaGang/MelonLoader) into your game, then launch the game once so it creates its folders.

**2.** Download the latest [release](../../releases), or build from source (see [Building](#building)).

**3.** Copy the following files into your game's directory:

| From the release | To |
|---|---|
| `BimosVrInjector.dll` | `<Game>/Mods/` |
| everything in `UserLibs/` | `<Game>/UserLibs/` |
| `rig.bundle` | `<Game>/UserData/BimosVrInjector/` |

Create `UserData/BimosVrInjector/` if it does not already exist.

**4.** For VR output, the game also needs OpenXR runtime files, which cannot be redistributed here. See [Setting up VR](wiki/Setting-up-VR.md), it is a one-time copy of four files from a Unity install.

### Limitations

- The Current state of the repository only works with BIMOS 1.0, BIMOS 2.0 support is still work in progress.
- AssetBundles are **forward compatible only**: a bundle loads in the Unity version it was built with, or newer. Each release states the version its bundle was built with. If your game is older than that, or you want a customised rig, build your own, see [Building the rig bundle](wiki/Building-the-rig-bundle.md).
- When no bundle is present at all, the mod spawns a placeholder primitive instead, which is enough to test the authoring loop.
- Spawning the rig does not connect it to the host game's systems. Movement, interaction and game logic still belong to the game's own player unless you manually modify it.
- Games built without VR have no stereo shader variants, so `MultiPass` rendering is required and costs roughly twice the GPU time, lowering fps significantly.
- IL2CPP support is work in progress and untested.


## Quick start

1. Launch the game.
2. Press **F5** to open author mode.
3. Stand where the VR player should be and press **Spawn Rig @ Camera**.
4. Select the game's player camera and press **Toggle Disable**.
5. Press **Save**, then reload the scene to confirm everything reapplies.

Full walkthrough: [Authoring a game](wiki/Authoring-a-game.md).

## Hotkeys

| Key | Action |
|---|---|
| `F5` | Toggle author mode |
| `F6` | Spawn rig at camera |
| `F7` | Save config |
| `F8` | Replay config now |
| `F9` | Start OpenXR |
| `F10` | Log XR and camera diagnostics |
| `F11` | Tag all physics bodies grabbable |
| `P` | Pick object under cursor |

## Configuration

`UserData/MelonPreferences.cfg`, section `[BimosVrInjector]`:

| Key | Default | Description |
|---|---|---|
| `RigBundleFile` | `rig.bundle` | Bundle filename in `UserData/BimosVrInjector/` |
| `RigPrefabName` | `""` | Prefab name in the bundle |
| `RigForceVrMode` | `true` | Force BIMOS 2.0 into VR rather than flatscreen mode |
| `RigIsolateOnSpawn` | `true` | Ignore collisions with dynamic bodies the rig spawns inside |
| `XrRenderMode` | `MultiPass` | `MultiPass` or `SinglePassInstanced` |
| `XrInteractionProfiles` | `auto` | Controller profiles to enable |
| `XrResolutionScale` | `1.0` | Eye render scale, lower for performance |
| `XrPhysicsRate` | `0` | Physics Hz, `0` matches the headset, `-1` leaves the game's rate |
| `XrAutoStartWithRig` | `true` | Start OpenXR before spawning the rig |
| `UserLibsSkip` | `""` | Assembly name fragments to skip preloading |


## Building

Reference assemblies are not committed. Copy them from your game and MelonLoader install into `libs/mono/`:

| File | Source |
|---|---|
| `MelonLoader.dll`, `0Harmony.dll` | `<Game>/MelonLoader/net35/` |
| `UnityEngine*.dll` | `<Game>/<Game>_Data/Managed/` |

Then:

```bash
dotnet build src/BimosVrInjector.Mod -c Mono
```

Output is staged to `build/mono/` in the layout described in [Install](#install). Run the test suite with:

```bash
dotnet run --project tests/BimosVrInjector.Core.Tests
```

Target framework is `net45` by default. If the game ships a .NET 3.5 profile (`mscorlib` 2.0 in `Managed/`), switch the Mono `TargetFramework` to `net35`. See [Building the mod](wiki/Building-the-mod.md).

## Project layout

| Path | Purpose |
|---|---|
| `src/BimosVrInjector.Core` | Config model, object resolution, playback logic. No engine dependencies. |
| `src/BimosVrInjector.Mod` | MelonLoader mod: scene hooks, UI, XR bootstrap, Unity implementations. |
| `tests/` | Test suite for the core pipeline, runs without a game. |
| `unity/` | Starter files for the rig bundle project. |

`Core` targets `net45` and `netstandard2.0` and has no reference to Unity, MelonLoader or UniverseLib, which keeps the config and resolution logic testable outside a game.

## Documentation

- [Authoring a game](wiki/Authoring-a-game.md)
- [Building the rig bundle](wiki/Building-the-rig-bundle.md)
- [Building the mod](wiki/Building-the-mod.md)
- [Setting up VR](wiki/Setting-up-VR.md)
- [Configuration reference](wiki/Configuration.md)
- [Troubleshooting](wiki/Troubleshooting.md)
- [How it works](wiki/How-it-works.md)
- [Packaging a release](wiki/Packaging-a-release.md)

## License

Copyright (C) 2026 Garebeu

Licensed under the **GNU General Public License v3.0** - see [LICENSE](LICENSE).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. It is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

Distributing a modified version means publishing your source under the same terms.

### Third-party components

Full notices in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Releases redistribute the BIMOS rig and its assemblies. BIMOS is MIT licensed and belongs to [KadenZombie8](https://github.com/KadenZombie8/BIMOS) - it remains under its own license, and its copyright and permission notice travel with every release. The same applies to UniverseLib, Newtonsoft.Json and HarmonyX (MIT) and MelonLoader (Apache-2.0), all of which are GPL-3.0 compatible.

Unity's XR assemblies are **not** redistributed. Their license only permits distribution integrated into your own Unity build, which a mod is not, and it is not GPL-compatible. Users supply them from their own Unity installation - see [Setting up VR](wiki/Setting-up-VR.md).

