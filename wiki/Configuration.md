# Configuration

## Preferences

`<Game>/UserData/MelonPreferences.cfg`, section `[BimosVrInjector]`. Written on first run; edit and restart the game.

### Rig

| Key | Default | Description |
|---|---|---|
| `RigBundleFile` | `rig.bundle` | Bundle filename inside `UserData/BimosVrInjector/`. Useful for keeping per-version bundles side by side. |
| `RigPrefabName` | `""` | Prefab asset name in the bundle. Empty uses the first GameObject found, which is correct for a single-prefab bundle. |
| `RigForceVrMode` | `true` | Sets BIMOS 2.x's control type to VR at spawn. Turn off only if you want its flatscreen mode. |
| `RigIsolateOnSpawn` | `true` | Disables collisions between the rig and dynamic bodies it spawns inside. Static geometry is never isolated. |

### XR

| Key | Default | Description |
|---|---|---|
| `XrRenderMode` | `MultiPass` | `MultiPass` or `SinglePassInstanced`. SinglePass is roughly twice as fast but needs stereo shader variants the game almost certainly lacks. |
| `XrInteractionProfiles` | `auto` | `auto` enables every controller profile the OpenXR package ships. Otherwise a comma-separated name filter, e.g. `OculusTouch,ValveIndex`. |
| `XrResolutionScale` | `1.0` | Eye render target scale. 0.7–0.8 is a large performance win for a modest sharpness cost. |
| `XrPhysicsRate` | `0` | Physics ticks per second. `0` follows the headset refresh rate, `-1` leaves the game's rate untouched, any other value is used directly. |
| `XrAutoStartWithRig` | `true` | Starts OpenXR before spawning the rig. Leave on, tracked-pose components bind on wake. |

### Loading

| Key | Default | Description |
|---|---|---|
| `UserLibsSkip` | `""` | Semicolon-separated assembly name fragments to skip preloading. Use when an assembly in `UserLibs/` destabilises the game. |

## Scene configs

One JSON file per scene in `<Game>/UserData/BimosVrInjector/configs/`, named after the scene.

```json
{
  "formatVersion": 1,
  "sceneName": "Level_01",
  "disable": [ ObjectKey, ... ],
  "delete": [ ObjectKey, ... ],
  "grabbable": [ { "target": ObjectKey } ],
  "rigSpawn": { "pos": [x, y, z], "rot": [x, y, z], "scale": [x, y, z] },
  "autoGrabAllBodies": false
}
```

| Field | Meaning |
|---|---|
| `disable` | Set inactive on load |
| `delete` | Destroyed on load |
| `grabbable` | Given a BIMOS grab component on load |
| `rigSpawn` | Rig transform; omit the field entirely to spawn no rig |
| `autoGrabAllBodies` | Tag every non-trigger collider with a Rigidbody as grabbable |

### Object keys

```json
{
  "path": "Player/Camera/FlatController",
  "name": "FlatController",
  "parentChain": ["Player", "Camera"],
  "siblingIndex": 2,
  "components": ["Transform", "FlatController"]
}
```

Deliberately redundant. Resolution tries the exact `path` first, then scores candidates that share the `name` on parent-chain agreement, component overlap and sibling index, accepting the best above a confidence threshold. This is what lets a config survive a game update that reorganises the hierarchy.

An unresolvable key logs a warning naming the object and is skipped. Playback never throws on a bad key.

Editing configs by hand is fine. `path` is the human-readable field; the rest is matching data. If you change `path`, keep the other fields consistent or resolution will fall back to fuzzy matching.
