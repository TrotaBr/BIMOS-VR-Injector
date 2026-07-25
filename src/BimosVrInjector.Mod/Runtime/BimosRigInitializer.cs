using System;
using System.Reflection;
using BimosVrInjector.Core.Abstractions;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal static class BimosRigInitializer
    {
        private const string RigTypeName = "KadenZombie8.BIMOS.Rig.BIMOSRig";
        private const string LocalRigTypeName = "KadenZombie8.BIMOS.Rig.LocalRig";

        public static void PrepareInstance(GameObject instance, ILog log)
        {
            try
            {
                var rigType = FindType(RigTypeName);
                if (rigType == null)
                    return;

                var rig = instance.GetComponentInChildren(rigType, true);
                if (rig == null)
                {
                    log.Warn("[rig] BIMOSRig component not found on the spawned prefab — " +
                             "is this really the BIMOS player rig?");
                    return;
                }

                var localRigType = FindType(LocalRigTypeName);
                if (localRigType == null)
                    return;

                var host = ((Component)rig).gameObject;
                if (host.GetComponent(localRigType) != null)
                {
                    log.Info("[rig] LocalRig already present.");
                    return;
                }

                host.AddComponent(localRigType);
                log.Info("[rig] added LocalRig — registers BIMOSUtils.LocalRig/.Settings " +
                         "(normally done by BIMOS's spawn-point flow, which an injector bypasses).");
            }
            catch (Exception ex)
            {
                log.Warn($"[rig] LocalRig setup failed: {ex.Message}");
            }
        }

        public static void ForceVrMode(ILog log)
        {
            try
            {
                var utilsType = FindType("KadenZombie8.BIMOS.Rig.BIMOSUtils");
                var settings = utilsType?
                    .GetProperty("Settings", BindingFlags.Public | BindingFlags.Static)?
                    .GetValue(null, null);
                if (settings == null)
                {
                    log.Warn("[rig] BIMOSUtils.Settings is null — cannot force VR mode.");
                    return;
                }

                var tryGet = settings.GetType().GetMethod("TryGetSetting");
                if (tryGet == null)
                    return;

                var args = new object?[] { "Debug_ControlType", null };
                if (!(bool)tryGet.Invoke(settings, args)! || args[1] == null)
                {
                    log.Warn("[rig] 'Debug_ControlType' setting not found (BIMOS 1.x?).");
                    return;
                }

                var setting = args[1]!;
                var settingType = setting.GetType();
                var valueProp = settingType.GetProperty("Value",
                    BindingFlags.Public | BindingFlags.Instance);
                var saveMethod = settingType.GetMethod("Save", Type.EmptyTypes);

                var current = valueProp?.GetValue(setting, null);
                if (current is int cur && cur == 0)
                {
                    log.Info("[rig] control type already VR.");
                    return;
                }

                valueProp?.SetValue(setting, 0, null);
                saveMethod?.Invoke(setting, null);
                log.Info($"[rig] control type {current} -> 0 (VR mode forced).");
            }
            catch (Exception ex)
            {
                log.Warn($"[rig] could not force VR mode: {ex.Message}");
            }
        }

        private static Type? FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName);
                    if (t != null)
                        return t;
                }
                catch {  }
            }
            return null;
        }
    }
}
