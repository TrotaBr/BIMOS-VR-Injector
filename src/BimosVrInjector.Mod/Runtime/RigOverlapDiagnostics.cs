using System.Collections.Generic;
using BimosVrInjector.Core.Abstractions;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal static class RigOverlapDiagnostics
    {
        public static void Report(GameObject rig, ILog log, bool isolate, float radius = 1.2f)
        {
            if (rig == null)
                return;

            try
            {
                var rigTransforms = new HashSet<Transform>();
                foreach (var t in rig.GetComponentsInChildren<Transform>(true))
                    rigTransforms.Add(t);

                var hits = Physics.OverlapSphere(rig.transform.position, radius,
                    ~0, QueryTriggerInteraction.Ignore);

                var offenders = new List<string>();
                var seenRoots = new HashSet<string>();
                var offendingRoots = new List<Transform>();
                var staticOverlaps = new List<string>();

                foreach (var col in hits)
                {
                    if (col == null || rigTransforms.Contains(col.transform))
                        continue;

                    var root = col.transform.root;
                    var label = root != null ? root.name : col.name;

                    if (label == AssetBundleRigSpawner.RigName)
                        continue;

                    if (!seenRoots.Add(label))
                        continue;

                    bool dynamic = col.attachedRigidbody != null;
                    if (dynamic)
                    {
                        if (root != null)
                            offendingRoots.Add(root);
                        offenders.Add($"{PathOf(col.transform)}  [dynamic, root '{label}']");
                    }
                    else
                    {
                        staticOverlaps.Add($"{PathOf(col.transform)}  [static, root '{label}']");
                    }
                }

                if (staticOverlaps.Count > 0)
                {
                    log.Warn($"[rig] spawn point is inside {staticOverlaps.Count} piece(s) of STATIC level " +
                             "geometry. NOT isolating those (that would disable floor collision and the " +
                             "locomotion sphere would never ground). Move the rig spawn to open space instead:");
                    for (int i = 0; i < staticOverlaps.Count && i < 6; i++)
                        log.Warn($"[rig]   {staticOverlaps[i]}");
                }

                if (offenders.Count == 0)
                {
                    if (staticOverlaps.Count == 0)
                        log.Info("[rig] spawn point is clear of other colliders.");
                    return;
                }

                log.Warn($"[rig] spawned INSIDE {offenders.Count} dynamic body group(s) — " +
                         "PhysX would eject the rig (the 'thrown around' symptom):");
                for (int i = 0; i < offenders.Count && i < 12; i++)
                    log.Warn($"[rig]   {offenders[i]}");

                if (isolate)
                    Isolate(rig, offendingRoots, log);
                else
                    log.Warn("[rig] isolation disabled — disable these objects in author mode, " +
                             "or set RigIsolateOnSpawn=true in MelonPreferences.cfg.");
            }
            catch (System.Exception ex)
            {
                log.Warn($"[rig] overlap check failed: {ex.Message}");
            }
        }

        private static void Isolate(GameObject rig, List<Transform> roots, ILog log)
        {
            var rigCols = rig.GetComponentsInChildren<Collider>(true);
            int pairs = 0;

            foreach (var root in roots)
            {
                if (root == null)
                    continue;
                var otherCols = root.GetComponentsInChildren<Collider>(true);
                foreach (var a in rigCols)
                {
                    if (a == null) continue;
                    foreach (var b in otherCols)
                    {
                        if (b == null) continue;
                        try
                        {
                            Physics.IgnoreCollision(a, b, true);
                            pairs++;
                        }
                        catch {  }
                    }
                }
                log.Info($"[rig] ignoring collisions with '{root.name}'.");
            }

            log.Info($"[rig] isolated {pairs} collider pair(s) — the rig no longer fights the " +
                     "game's own player. The game's player stays functional.");
        }

        private static string PathOf(Transform t)
        {
            var s = t.name;
            for (var p = t.parent; p != null; p = p.parent)
                s = p.name + "/" + s;
            return s;
        }
    }
}
