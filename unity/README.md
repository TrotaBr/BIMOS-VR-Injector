# unity/

Starter files for the rig bundle project. Copy the contents of `Assets/` into your
Unity project's `Assets/` folder, preserving the layout:

```
<UnityProject>/Assets/Editor/BuildRigBundle.cs
<UnityProject>/Assets/BIMOS.Runtime/BIMOS.Runtime.asmdef
<UnityProject>/Assets/BIMOS.Runtime/RotateForever.cs
```

`BuildRigBundle.cs` adds a **BIMOS → Build rig.bundle (Windows64)** menu item.

`RotateForever.cs` is a trivial test component: put it on a prefab, bundle it, and
if it spins in game then bundle-to-assembly script resolution is working. The
assembly definition keeps it out of `Assembly-CSharp`, which would collide with the
target game's own assembly of that name.

See `wiki/Building-the-rig-bundle.md`.
