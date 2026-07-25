# Troubleshooting

Start with the MelonLoader log (`<Game>/MelonLoader/Latest.log`) and press **F10** in game for an XR and camera dump. Most problems here announce themselves clearly if you know what the line means.

## The mod does not load

**`TypeLoadException` on startup, or types reported as invalid.** The mod's target framework does not match the game's runtime. Check the game's `Managed/` folder:

- `mscorlib.dll` is 4.x and there is **no** `netstandard.dll` → build as `net45` (the default).
- `mscorlib.dll` is 2.0 and `UnityEngine.UI.dll` is also a 2.0 image → build as `net35`.

MelonLoader's `Runtime Type: net35` log line is not a reliable signal; it says that for every Mono game.

**Nothing appears in the log at all.** Confirm MelonLoader itself works with no mods installed. A broken loader install produces native crashes that look like mod bugs.

## Scripts on the rig are missing

**`The referenced script (...) is missing!` in `Player.log`.** The bundle's script references cannot be resolved.

- The assembly is not in `UserLibs/`. Check the console for `Preloaded UserLibs assembly: <name>` , if your assembly is not listed, it is not there.
- The assembly is stale relative to the bundle. Rebuild both together after updating BIMOS.
- The scripts compiled into `Assembly-CSharp`, which collides with the game's own. They need their own assembly definition.

## Controllers do not work

**F10 shows `legacy XR InputDevices: 1` (head only).** No interaction profiles are registered. Look earlier in the log for:

```
[XR] interaction profile setup failed: ...
```

This usually means a dependency of the OpenXR assembly is missing, most often `Unity.XR.CompositionLayers.dll`. Add it to `UserLibs/`.

**Devices are listed but hands do not move.** The rig was spawned before XR started, so the tracked-pose components had nothing to bind to. Respawn the rig with XR already running; `XrAutoStartWithRig` does this automatically.

## Hands float in front of you and ignore the controllers

BIMOS 2.x is in flatscreen mode, where hands are driven by a palm-pose emulator rather than tracking. `RigForceVrMode` (default on) sets the control type to VR at spawn. Confirm with:

```
[rig] control type already VR.
```

## The rig is thrown around violently

The rig spawned inside something solid and PhysX is ejecting it. The log names the offenders:

```
[rig] spawned INSIDE 1 dynamic body group(s) ...
[rig]   Character [...]/Torso/RigCollider  [dynamic, root 'Character [...]']
```

**Dynamic bodies** (the game's character, loose props) are handled automatically , collisions between them and the rig are disabled, leaving the game's player functional.

**Static level geometry** is reported but deliberately *not* isolated:

```
[rig] spawn point is inside static level geometry. NOT isolating those ...
```

Disabling collision with the level would also disable the floor, and the rig would never stand on anything. Move to open ground and respawn instead.

## The rig sinks through the floor

Something disabled collision with the level. If the log shows `ignoring collisions with 'Map'` you are on an old build , update. Otherwise check that you have not disabled the level's colliders in the config.

## One eye grey, one eye black

`SinglePassInstanced` rendering in a game without stereo shader variants. Set `XrRenderMode = MultiPass`.

## Everything is slow

MultiPass renders the scene twice, and the game is now running at headset resolution and framerate. Lower `XrResolutionScale` to 0.7–0.8, and drop the game's own graphics settings.

## The game crashes with no managed exception

A native crash. Check the Windows Event Viewer (Application log) for the faulting module , `UnityPlayer.dll` or `ntdll.dll` with code `0xc0000005` is an access violation. Likely causes, in order:

1. An assembly in `UserLibs/` installing subsystem or render hooks the game was never built for. AR Foundation, XR Simulation and Composition Layers are the usual suspects. Remove them, or use `UserLibsSkip`, and add back only what is needed.
2. A bundle built with a **newer** Unity than the game. The mod warns about this at load; see [Building the rig bundle](Building-the-rig-bundle.md).
3. The base loader and game combination, with no mods involved. Test with the mod removed before assuming it is the mod.

## A saved object no longer resolves

```
[disable] could not resolve 'X/Y/Z' -> no object named 'Z' in scene. Skipping.
```

The game updated and the object moved or was renamed. Resolution falls back from exact path to a scored match on name, parent chain, components and sibling index; below the confidence threshold it gives up rather than acting on the wrong object. Reopen author mode and re-select it.

A resolution that succeeded but was not exact is also logged, so you can check it picked the right thing:

```
[disable] resolved 'X/Y/Z' by fuzzy match on 'Z' (confidence 0.85) , verify this is correct.
```
