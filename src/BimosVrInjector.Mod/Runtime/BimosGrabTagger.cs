using System;
using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Resolve;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class BimosGrabTagger : IGrabTagger
    {
        private static readonly string[] AutoGrabTypeNames =
        {
            "KadenZombie8.BIMOS.Rig.AutoGrabbable",
            "BIMOS.AutoGrab",
        };

        private static readonly string[] GrabBaseTypeNames =
        {
            "KadenZombie8.BIMOS.Rig.Grabbable",
            "BIMOS.Grab",
        };

        private readonly ILog _log;
        private Type? _autoGrabType;
        private Type? _grabType;
        private bool _lookedUp;
        private bool _warned;

        public BimosGrabTagger(ILog log)
        {
            _log = log;
        }

        public void Tag(ITreeNode node, GrabbableEntry entry)
        {
            var go = ((UnityTreeNode)node).Go;
            if (go == null)
                return;

            if (!EnsureTypes())
                return;

            if (TagObject(go))
                _log.Info($"[grab] tagged '{ObjectKey.From(node)}' with AutoGrab.");
        }

        public int AutoTagAllBodies()
        {
            if (!EnsureTypes())
                return 0;

            int tagged = 0;
            int rigLayer = BimosLayerFix.RigLayer;

            var colliders = UnityEngine.Object.FindObjectsOfType<Collider>();
            foreach (var col in colliders)
            {
                if (col == null || col.isTrigger)
                    continue;
                if (col.attachedRigidbody == null)
                    continue;
                if (rigLayer >= 0 && col.gameObject.layer == rigLayer)
                    continue;

                if (TagObject(col.gameObject))
                    tagged++;
            }

            return tagged;
        }

        private bool TagObject(GameObject go)
        {
            try
            {
                if (_grabType != null && go.GetComponent(_grabType) != null)
                    return false;

                go.AddComponent(_autoGrabType);
                return true;
            }
            catch (Exception ex)
            {
                if (!_warned)
                {
                    _log.Warn($"[grab] could not add AutoGrab to '{go.name}': {ex.Message}");
                    _warned = true;
                }
                return false;
            }
        }

        private static Type? FindFirstType(string[] candidates)
        {
            foreach (var name in candidates)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var t = asm.GetType(name);
                        if (t != null)
                            return t;
                    }
                    catch {  }
                }
            }
            return null;
        }

        private bool EnsureTypes()
        {
            if (_lookedUp)
                return _autoGrabType != null;
            _lookedUp = true;

            _autoGrabType = FindFirstType(AutoGrabTypeNames);
            _grabType = FindFirstType(GrabBaseTypeNames);

            if (_autoGrabType == null)
            {
                _log.Error($"[grab] none of [{string.Join(", ", AutoGrabTypeNames)}] found — is " +
                           "kadenzombie8.bimos.dll in UserLibs, and does its version match? " +
                           "Grab tagging disabled for this session.");
            }
            else
            {
                _log.Info($"[grab] using '{_autoGrabType.FullName}'.");
            }
            return _autoGrabType != null;
        }
    }
}
