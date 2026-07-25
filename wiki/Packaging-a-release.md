# Packaging a release

A release is a zip a user can extract straight into their game folder, plus a rig that works out of the box.

## Contents

```
BimosVrInjector-<version>.zip
├── Mods/
│   └── BimosVrInjector.dll
├── UserLibs/
│   ├── BimosVrInjector.Core.dll
│   ├── Newtonsoft.Json.dll
│   ├── UniverseLib.Mono.dll
│   └── kadenzombie8.bimos.dll
├── UserData/
│   └── BimosVrInjector/
│       └── rig.bundle
├── README.md
├── LICENSE
└── THIRD-PARTY-NOTICES.md
```

Mirroring the game's folder layout means users can drag the contents in rather than following instructions per file.

## Building the parts

**Mod:**

```bash
dotnet build src/BimosVrInjector.Mod -c Mono
```

Staged to `build/mono/`.

**Rig:** see [Building the rig bundle](Building-the-rig-bundle.md). Build it with the **oldest** Unity 6 version you intend to support , forward compatibility means older covers more games.

**BIMOS assemblies:** take them from the `Managed` folder of a **player build**, never from `Library/ScriptAssemblies`. Editor-compiled assemblies reference `UnityEditor` and fail at runtime.

Only ship BIMOS assemblies the rig actually needs. BIMOS 1.x needs `kadenzombie8.bimos.dll` and nothing else , Input System, Animation Rigging and TextMeshPro come from the game.

## Do not ship

- **Unity XR assemblies and native plugins.** `Unity.XR.*` and `UnityOpenXR.dll` are Unity Technologies components. The Unity Package Distribution License only permits distributing them integrated into your own Unity build, which a mod is not, and those terms are not GPL-compatible. Users copy them from their own Unity install; [Setting up VR](Setting-up-VR.md) explains how.
  (`openxr_loader.dll` is the Khronos loader under Apache-2.0 and *could* be shipped with attribution, but there is little point shipping one file of four.)
- **Game assemblies.** Anything from `libs/`.

## Before publishing

- [ ] `LICENSE` (GPL-3.0) is included in the zip. GPL-3.0 requires the license text to accompany every distributed copy.
- [ ] The BIMOS `LICENSE` text is pasted into `THIRD-PARTY-NOTICES.md`. A link is not sufficient under MIT , the notice must accompany the software.
- [ ] The release notes link to the source repository. GPL-3.0 requires recipients to be able to obtain the corresponding source.
- [ ] The release notes state the **Unity version the bundle was built with**, so users can tell whether it covers their game.
- [ ] The release notes state which **BIMOS version** the rig is.
- [ ] UniverseLib's license terms have been checked for the version being shipped.
- [ ] Tested on a clean game install: extract, launch, spawn the rig, reload the scene.

## Versioning

Bump `MelonInfo` in `src/BimosVrInjector.Mod/ModEntry.cs` to match the release tag. It is what the log prints, which is the first thing anyone reporting a problem will paste.
