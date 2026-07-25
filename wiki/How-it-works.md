# How it works

## Structure

Two assemblies, split along a hard line.

**`BimosVrInjector.Core`** holds the config model, object keys, the resolver, the operation log and the playback applier. It references nothing from Unity, MelonLoader or UniverseLib, and multi-targets `net45` and `netstandard2.0`. Everything it does is expressed against small interfaces , `ISceneAccess`, `IRigSpawner`, `IGrabTagger`, `ILog` , so the whole config and resolution pipeline runs in a plain console test harness with fakes, no game required.

**`BimosVrInjector.Mod`** is the MelonLoader mod: scene hooks, the UniverseLib panel, XR bootstrap, and the Unity-backed implementations of Core's interfaces.

The split exists because the interesting logic is the part that has nothing to do with Unity, and testing it inside a running game is slow and unreliable.

## Author, config, replay

Author actions append to an in-memory operation log rather than mutating a config directly. Saving snapshots that log into a `SceneConfig` and writes it as JSON. On scene load, playback reads the config for that scene and applies it through the same interfaces.

Application order matters: deletions first, then disables, then the rig, then grab tags, with a scene refresh between destructive passes so resolution sees current state.

## Object identity

The problem: a config written today must still work after the game updates.

A raw hierarchy path breaks the moment anything is renamed or reparented. So a key stores the path *plus* the object name, its parent chain, its sibling index, and the type names of its components.

Resolution tries the exact path first. Failing that, it scores every object sharing the name , parent chain agreement weighted highest, then component overlap, then sibling index , and accepts the best match above a confidence threshold. Below that it refuses, logs which object it could not find and why, and moves on. Acting on the wrong object is worse than skipping.

Non-exact matches are logged with their confidence so a human can sanity check them.

## Runtime obstacles

Most of the mod's complexity is not the pipeline; it is the things a game actively does not expect.

**Named layers that do not exist.** BIMOS 1.x resolves `BIMOSRig` and `BIMOSMenu` by name at runtime, and uses the results for its own collision matrix and camera culling. A host game has never heard of those names, so `NameToLayer` returns `-1` , and assigning layer `-1` throws, aborting the rig's initialisation partway through. The mod patches `LayerMask.NameToLayer` and `GetMask` to resolve those two names to layer indices that are genuinely free in the running game. Layer numbers are arbitrary; only the semantics matter.

**Assemblies that never load.** Unity binds a bundle's script references against assemblies already loaded in the process. An assembly that nothing references , the rig's scripts, for instance , is never loaded, so every component on the prefab resolves to a missing script. Some MelonLoader versions load `UserLibs` eagerly and some do not, so the mod force-loads them itself.

**Initialisation order.** Components read their dependencies when they wake. BIMOS 2.x expects a `LocalRig` component to have registered the rig and created its settings object before anything else starts. The prefab does not carry that component when spawned directly rather than through BIMOS's own spawn flow. The mod instantiates the prefab **inactive**, injects what is missing, then activates it , adding the component after activation would be too late.

**Missing XR infrastructure.** Covered in [Setting up VR](Setting-up-VR.md): MultiPass rendering, interaction profiles, the XR Management singleton, and physics rate.

**Spawning inside things.** A rig placed at the flat game's camera lands inside that game's player. PhysX resolves the overlap by ejecting one of them. The mod reports what it overlapped and disables collision against dynamic bodies only , never static level geometry, since that would take the floor with it.

## Design constraints

Two rules shaped the tool:

**No automatic detection.** The mod never guesses which object is the player or which objects should be grabbable. Those are judgement calls, and a wrong guess produces a broken game with no obvious cause. The bulk grab tagger is the one deliberate exception, off by default.

**Nothing persists.** Unity rebuilds the scene on every load. The config is not a save file to be restored once; it is a set of operations reapplied from scratch every time. Everything is written to be idempotent.
