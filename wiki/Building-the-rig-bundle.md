# Building the rig bundle

Releases include a prebuilt BIMOS rig, so most people never need this page.

## Do you need to build one?

You do **not** if the included rig loads in your game, check the log for the bundle version line, and see [Do I need my own rig?](#do-i-need-my-own-rig) below.

You **do** if any of these apply:

- Your game runs an **older** Unity version than the included bundle was built with. Bundles are forward compatible only.
- You want a different BIMOS version, or a rig you have modified, different hand poses, body proportions, attached props.
- You are packaging a release of this mod and need to produce the bundle that ships with it.

If no bundle is present at all, the mod spawns a placeholder primitive. That is enough to prove the authoring loop end to end before you involve Unity.

## Do I need my own rig?

The mod reads the Unity version out of the bundle header at load and reports it:

```
[bundle] built with Unity 6000.0.23f1, running 6000.3.15f1 (older->newer is the supported direction).
```

That is fine. This is not:

```
[bundle] built with Unity 6000.3.18f1 but this game runs 6000.3.15f1. A NEWER bundle
in an OLDER player is the unsupported direction and can crash natively.
```

## Version rule

**Build with the game's Unity version, or an older one. Never a newer one.**

AssetBundles are forward compatible only. A bundle written by an older editor loads in a newer player; the reverse can fail in native deserialization, which surfaces as an unexplained crash rather than an error message. The mod reads the version out of the bundle header and warns on mismatch:

```
[bundle] built with Unity 6000.3.18f1 but this game runs 6000.3.15f1.
```

The game's version is in the MelonLoader log header. Install the matching editor from the Unity Hub archive.

### Choosing a version for a public release

If you are building the bundle that ships with a release, build it with the **oldest** Unity version you intend to support, not the newest you happen to have installed. Forward compatibility means an older bundle covers a wider range of games, a bundle built with the first Unity 6 LTS loads in every later Unity 6 game, while one built with the newest patch release covers almost nothing.

BIMOS requires Unity 6, so `6000.0` LTS is the practical floor. State the version on the release page so users can tell at a glance whether it covers their game.

## What a bundle can and cannot carry

A bundle carries serialized objects: prefabs, meshes, materials, animation data. It carries **no code**. Every MonoBehaviour is stored as a reference to `(assembly name, namespace, class)` plus its field values, and those references are resolved at load time against assemblies already loaded in the process.

So BIMOS's scripts have to reach the game as **managed DLLs in `UserLibs/`**. Without them, every scripted component on the rig loads as a missing script and you get an inert shell.

## Project setup

1. Create a Unity project on the game's version. Use the **Universal 3D** template if the game uses URP.
2. Install BIMOS. Version 1.x is currently the better-tested target for injection; 2.x works but has a larger dependency surface.
3. Copy `unity/Assets/` from this repository into the project's `Assets/` folder. It provides the build menu item and an assembly definition.

### Assembly identity

If BIMOS scripts compile into the default `Assembly-CSharp`, the bundle will reference *the game's* `Assembly-CSharp`, where those classes do not exist. They must live in their own assembly.

Installing BIMOS as a package handles this, it ships its own `kadenzombie8.bimos` asmdef. If you instead drop the source into `Assets/`, put it under a folder with an assembly definition.

## Building

1. Select the BIMOS player rig prefab in the Project window.
2. At the bottom of the Inspector, set its AssetBundle name to `rig.bundle`.
3. Menu → **BIMOS → Build rig.bundle (Windows64)**.

Output lands in `AssetBundles/`. Only `rig.bundle` matters; the `.manifest` files are build metadata.

## Shipping it

| File | From | To |
|---|---|---|
| `rig.bundle` | `AssetBundles/` | `<Game>/UserData/BimosVrInjector/` |
| `kadenzombie8.bimos.dll` | `<build>_Data/Managed/` | `<Game>/UserLibs/` |
| any other BIMOS assemblies | `<build>_Data/Managed/` | `<Game>/UserLibs/` |

**Take managed DLLs from a player build, not from `Library/ScriptAssemblies`.** ScriptAssemblies are compiled with `UNITY_EDITOR` defined; some of them reference `UnityEditor` assemblies that do not exist in a shipped game, and they fail at runtime with `FileNotFoundException: UnityEditor.CoreModule`. Make a throwaway Windows build and harvest from its `Managed` folder.

Dependencies the game already ships (Input System, Animation Rigging, TextMeshPro, URP) do not need copying. Check what the BIMOS assembly actually references and only supply what is missing.

If a build fails on a missing `WindowsPlayer.exe`, tick **Development Build**, that variation is often intact when the release one is not.

## Verifying incrementally

When something does not work it is much easier to find out why if you have proven each layer separately:

1. **A capsule with no scripts.** Proves the version matches, the bundle loads, and the transform is applied.
2. **A capsule with one trivial script**, compiled into its own assembly and copied to `UserLibs/`. Proves script references resolve.
3. **The real BIMOS rig.**

Each rung takes minutes and eliminates a whole category of failure. The console tells you which one broke.

## Keeping the bundle current

If you change the rig prefab, the bundle only rebuilds if Unity sees the change on disk. Save the prefab (Ctrl+S in Prefab Mode, or **Overrides → Apply All** when editing an instance in a scene) before building, and confirm the timestamp on `rig.bundle` actually moved.

When you update BIMOS itself, rebuild **both** the bundle and the assemblies. A bundle built against a newer version of the scripts than the DLL you shipped produces missing-script errors that look like corruption but are just a version skew.
