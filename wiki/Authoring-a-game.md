# Authoring a game

Author mode is where you make the decisions. Everything you do is recorded and replayed on later loads, so the goal is to get one scene right and save it.

## Opening the panel

Load into the game, then press **F5**. The mouse unlocks and the author panel appears. Press F5 again to return to playback mode.

While author mode is open, configs are not applied on scene load, you are editing the raw scene. Switching back to playback is what makes saved configs take effect again.

## Finding objects

Two ways to select something:

**Browse.** The list shows the children of the current node. Click a row to select it, **Enter ▶** to descend into it, **◄ Up** to go back. The breadcrumb above shows where you are.

**Point at it.** Aim at an object in the world and press **P**. This raycasts and selects whatever you hit, which is usually faster than hunting through the hierarchy for scenery and props.

The selection box shows the object's name, full path, and every component on it. Component names are the reliable way to identify what you have selected, a `Camera`, a `Rigidbody`, a game-specific controller script.

Row markers: disabled,  marked for deletion, tagged grabbable.

## Disabling the flat player

This is almost always the first real step. A flatscreen game has a camera and a player controller that will fight the VR rig for control of the view.

1. Find the game's main camera. `P` while looking at anything, then walk up the hierarchy with **◄ Up**, is often quicker than browsing from the root.
2. Select it and press **Toggle Disable**. It vanishes immediately so you can confirm you picked the right one.
3. Do the same for HUD canvases and any first-person body meshes.

Disable is recorded and reapplied later. It is reversible: toggle again to remove it from the config.

**Deletion** is available for objects that must not exist at all, but disabling is almost always enough and is easier to undo. Deletion is deferred, the object is destroyed on replay, not while you are authoring, so you do not lose your selection.

## Placing the rig

Stand where the VR player should be, facing the direction they should face, then press **Spawn Rig @ Camera** (or **F6**).

The rig spawns at the current camera pose, and OpenXR is started first automatically if it is not already running, tracked-pose components bind to devices when they wake, so starting XR afterwards leaves the hands unbound.

Watch the console after spawning:

```
[rig] spawn point is clear of other colliders.
```

That is what you want. If instead you see a list of collider groups, read [Troubleshooting](Troubleshooting.md#the-rig-is-thrown-around-violently), spawning inside the game's character or inside level geometry causes very visible physics problems.

Spawn **once** and evaluate. Repeatedly respawning stacks up teardown and re-initialisation and makes it hard to tell what is actually wrong.

## Tagging grabbables

Select an object with a Rigidbody and press **Toggle Grabbable**. On replay, BIMOS's `AutoGrab` component is attached to it. `AutoGrab` aligns the hand from the collider surface, so it needs no authored hand pose.

For a whole scene at once, **Auto-grab ALL bodies** tags every non-trigger collider that has a Rigidbody. This is off by default and is deliberately blunt: it will also tag scenery, vehicles, ragdolls and anything the game's own logic owns. Try it with **Apply now** first to see how the game reacts before committing it to the config.

Objects that already have a grab component are skipped, and the rig's own colliders are never tagged.

## Saving and verifying

**Save** writes `UserData/BimosVrInjector/configs/<scene>.json`.

Two ways to check it works:

- **Test Replay** (**F8**) reapplies the saved config immediately.
- Leave author mode and reload the scene. This is the real test, it is the path a player takes.

Reloading is worth doing at least once per scene, because it is the only way to confirm the config survives the thing it exists to survive.

## Per-scene work

Configs are keyed by scene name. A game with a menu, a hub and three levels needs a config per scene where you want VR. Reopening author mode in a scene that already has a config loads it, so you can refine rather than start over.
