# Setting up VR

A flatscreen game ships none of Unity's XR machinery. That machinery is just files, version-matched to the engine, so it can be harvested from a Unity project that has OpenXR installed and dropped into the game. The mod then initialises the loader at runtime.

This is the same general technique universal VR injectors use.

## Harvesting the files

From a Unity project on the game's version with the OpenXR package installed:

### Native files , straight from the package cache, no build needed

| From `<project>/Library/PackageCache/com.unity.xr.openxr@*/` | To |
|---|---|
| `Runtime/windows/x64/UnityOpenXR.dll` | `<Game>/<Game>_Data/Plugins/x86_64/` |
| `RuntimeLoaders/windows/x64/openxr_loader.dll` | `<Game>/<Game>_Data/Plugins/x86_64/` |
| `Runtime/UnitySubsystemsManifest.json` | `<Game>/<Game>_Data/UnitySubsystems/UnityOpenXR/` |

Use `windows/x64`, not `universalwindows` , that is the UWP build.

These are discovered by the engine at startup, so they must be in place before launching.

### Managed files , from a player build

| From `<build>_Data/Managed/` | To |
|---|---|
| `Unity.XR.Management.dll` | `<Game>/UserLibs/` |
| `Unity.XR.OpenXR.dll` | `<Game>/UserLibs/` |
| `Unity.XR.CoreUtils.dll` | `<Game>/UserLibs/` |
| `UnityEngine.SpatialTracking.dll` | `<Game>/UserLibs/` |

These **must** come from a player build, not `Library/ScriptAssemblies`. Editor-compiled copies contain the `UNITY_EDITOR` code paths and throw `FileNotFoundException: UnityEditor.CoreModule` the moment `OpenXRSettings.Instance` is touched.

Newer OpenXR packages may also need `Unity.XR.CompositionLayers.dll`. If the log shows a type-loading error mentioning `OpenXRDefaultLayer` or `ILayerHandler`, that assembly is the missing piece.

Before enabling OpenXR in the donor project, add your headset's **interaction profiles** under XR Plug-in Management → OpenXR. The mod enables profiles at runtime, but having them in the project keeps the two consistent.

## Starting it

With the OpenXR runtime already running (SteamVR open, or Quest Link connected and the headset awake):

1. Launch the game and load in.
2. Press **F9**, or just spawn the rig , XR starts automatically first.

Expected:

```
[XR] render mode = MultiPass
[XR] enabled 9 interaction profile(s): ...
[XR] Initialize OK.
[XR] XR Management wired (Instance=ok)
[XR] physics rate 60 Hz -> 72 Hz
[XR] OpenXR RUNNING
```

Press **F10** at any point for a dump of XR state, active cameras, and every input device from both input stacks. This is the first thing to read when something is wrong.

## What the mod sets up for you

**MultiPass rendering.** A game built without VR has no stereo-instancing shader variants. `SinglePassInstanced` needs them and produces one grey eye and one black eye without them. MultiPass renders each eye separately with ordinary shaders. It costs roughly double the GPU time, which is the price of working at all here. Lower `XrResolutionScale` to about 0.7–0.8 to claw some back.

**Interaction profiles.** A normal VR build bakes the chosen controller profiles into its settings. A runtime-created settings object has none, and OpenXR then advertises no controllers at all , head tracking works, hands do nothing. The mod instantiates every controller profile the package ships and registers them before initialising.

**XR Management wiring.** Some package code, including BIMOS's own physics-rate helper, reaches for `XRGeneralSettings.Instance.Manager.activeLoader`. That singleton does not exist in a non-VR game. The mod builds it around the loader it created so that code works instead of silently failing.

**Physics rate.** Physics-driven rigs are joint and PD-controller based, and they become unstable at a typical flatscreen game's tick rate. The mod matches `Time.fixedDeltaTime` to the headset refresh rate. Override with `XrPhysicsRate`.

## Limits

Getting the headset displaying and tracking is one problem; making the game playable in VR is another. The rig renders and tracks, but the game's own player still exists and still responds to keyboard and mouse. Connecting the VR rig to the game's movement and interaction systems is per-game work this tool does not attempt.
