using System;
using System.Collections.Generic;
using BimosVrInjector.Core.Abstractions;
using HarmonyLib;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal static class BimosLayerFix
    {
        public const string RigLayerName = "BIMOSRig";
        public const string MenuLayerName = "BIMOSMenu";

        private static ILog? _log;
        private static int _rigLayer = -1;
        private static int _menuLayer = -1;
        private static bool _resolved;

        public static int RigLayer => Resolve() ? _rigLayer : -1;
        public static int MenuLayer => Resolve() ? _menuLayer : -1;

        public static void Apply(HarmonyLib.Harmony harmony, ILog log)
        {
            _log = log;
            try
            {
                var nameToLayer = AccessTools.Method(typeof(LayerMask), "NameToLayer", new[] { typeof(string) });
                var getMask = AccessTools.Method(typeof(LayerMask), "GetMask", new[] { typeof(string[]) });

                if (nameToLayer != null)
                    harmony.Patch(nameToLayer,
                        prefix: new HarmonyMethod(typeof(BimosLayerFix), nameof(NameToLayerPrefix)));
                if (getMask != null)
                    harmony.Patch(getMask,
                        prefix: new HarmonyMethod(typeof(BimosLayerFix), nameof(GetMaskPrefix)));

                log.Info("[layers] BIMOS layer-name interception installed " +
                         $"(NameToLayer={nameToLayer != null}, GetMask={getMask != null}).");
            }
            catch (Exception ex)
            {
                log.Error($"[layers] could not patch LayerMask ({ex.Message}). BIMOS will see " +
                          "layer -1 and misbehave (rig raycasts hit itself, menu camera blank).");
            }
        }

        private static bool NameToLayerPrefix(string layerName, ref int __result)
        {
            if (layerName == RigLayerName && Resolve()) { __result = _rigLayer; return false; }
            if (layerName == MenuLayerName && Resolve()) { __result = _menuLayer; return false; }
            return true;
        }

        private static bool GetMaskPrefix(string[] layerNames, ref int __result)
        {
            if (layerNames == null || layerNames.Length == 0)
                return true;

            int mask = 0;
            bool anyOurs = false;
            foreach (var name in layerNames)
            {
                if (name == RigLayerName && Resolve()) { mask |= 1 << _rigLayer; anyOurs = true; }
                else if (name == MenuLayerName && Resolve()) { mask |= 1 << _menuLayer; anyOurs = true; }
                else
                {
                    if (anyOurs) continue;
                    return true;
                }
            }

            if (!anyOurs)
                return true;

            __result = mask;
            return false;
        }

        private static bool Resolve()
        {
            if (_resolved)
                return _rigLayer >= 0 && _menuLayer >= 0;
            _resolved = true;

            try
            {
                var used = 0;
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in all)
                {
                    if (go != null)
                        used |= 1 << go.layer;
                }

                var free = new List<int>();
                for (int i = 31; i >= 8; i--)
                {
                    if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                        continue;
                    if ((used & (1 << i)) != 0)
                        continue;
                    free.Add(i);
                    if (free.Count == 2)
                        break;
                }

                if (free.Count < 2)
                {
                    _log?.Error($"[layers] only {free.Count} free layer(s) available; BIMOS needs 2. " +
                                "The rig may collide with itself or the menu may not render.");
                    if (free.Count == 1) { _rigLayer = free[0]; _menuLayer = free[0]; }
                    return free.Count > 0;
                }

                _rigLayer = free[0];
                _menuLayer = free[1];
                _log?.Info($"[layers] BIMOSRig -> layer {_rigLayer}, BIMOSMenu -> layer {_menuLayer} " +
                           "(both unnamed and unused in this game).");
                return true;
            }
            catch (Exception ex)
            {
                _log?.Error($"[layers] free-layer scan failed: {ex.Message}");
                return false;
            }
        }
    }
}
