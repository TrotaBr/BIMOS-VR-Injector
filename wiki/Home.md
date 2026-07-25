# BIMOS VR Injector Wiki

Turning a flatscreen Unity game into VR with the BIMOS physics rig, one manual decision at a time.

## Start here

| Page | What it covers |
|---|---|
| [Authoring a game](Authoring-a-game.md) | The main workflow: disable, place, tag, save, replay |
| [Building the rig bundle](Building-the-rig-bundle.md) | Getting BIMOS out of Unity and into the game |
| [Building the mod](Building-the-mod.md) | Compiling from source, choosing the right target framework |
| [Setting up VR](Setting-up-VR.md) | Installing the OpenXR runtime files into a non-VR game |
| [Configuration](Configuration.md) | Every preference and what it does |
| [Troubleshooting](Troubleshooting.md) | Symptoms, causes, fixes |
| [How it works](How-it-works.md) | Architecture, and why the tricky parts are the way they are |
| [Packaging a release](Packaging-a-release.md) | Assembling a distributable zip, and what not to ship |

## The idea in one paragraph

Converting a game to VR is a series of judgement calls: which camera to kill, where the player should stand, which objects should be grabbable. Those calls need a human. What does not need a human is making the same calls again every time a scene loads. This tool splits the two: you make the decisions once through an in-game UI, and they are recorded to a JSON file that is replayed automatically from then on. The file is the mod you ship.

## Order of work

1. Install MelonLoader and the mod. A release includes a prebuilt BIMOS rig, so there is nothing to build unless you want to.
2. [Set up VR](Setting-up-VR.md) so OpenXR can start, a one-time copy of four files from a Unity install.
3. [Author the game](Authoring-a-game.md) scene by scene.

Optional, and only if you need them:

- [Build the mod](Building-the-mod.md) from source instead of using a release.
- [Build a rig bundle](Building-the-rig-bundle.md) if your game is on an older Unity version than the included rig, or you want a customised rig.

Steps 1–2 happen once per game. Step 3 is the ongoing work.

## Scope

This tool spawns and configures a VR rig inside a running game. It does not make the game's own systems aware of that rig. Movement, climbing, inventory, damage and networking still belong to the game's original player unless you connect them yourself, which is game-specific work outside what an injector can generalise.
