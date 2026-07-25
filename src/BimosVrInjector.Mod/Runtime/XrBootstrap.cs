using System;
using System.Reflection;
using BimosVrInjector.Core.Abstractions;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class XrBootstrap
    {
        private readonly ILog _log;
        private UnityEngine.Object? _loader;
        private Type? _loaderType;
        public bool Running { get; private set; }

        public string RenderMode { get; set; } = "MultiPass";

        public string InteractionProfiles { get; set; } = "auto";

        public float ResolutionScale { get; set; } = 1.0f;

        public float PhysicsRate { get; set; } = 0f;

        public XrBootstrap(ILog log)
        {
            _log = log;
        }

        public void TryStart()
        {
            if (Running)
            {
                _log.Info("[XR] already running.");
                return;
            }

            try
            {
                var openXrAsm = FindAssembly("Unity.XR.OpenXR");
                var mgmtAsm = FindAssembly("Unity.XR.Management");
                if (openXrAsm == null || mgmtAsm == null)
                {
                    _log.Error("[XR] managed XR assemblies not loaded. Needed in UserLibs: " +
                               $"Unity.XR.OpenXR ({(openXrAsm == null ? "MISSING" : "ok")}), " +
                               $"Unity.XR.Management ({(mgmtAsm == null ? "MISSING" : "ok")}). " +
                               "See docs/PHASE-XR.md for the harvest steps.");
                    return;
                }

                _loaderType = openXrAsm.GetType("UnityEngine.XR.OpenXR.OpenXRLoader");
                var settingsType = openXrAsm.GetType("UnityEngine.XR.OpenXR.OpenXRSettings");
                if (_loaderType == null || settingsType == null)
                {
                    _log.Error($"[XR] types missing: OpenXRLoader={(_loaderType != null)}, " +
                               $"OpenXRSettings={(settingsType != null)}.");
                    return;
                }

                EnsureSettingsInstance(settingsType);
                ApplyRenderMode(settingsType);
                EnableInteractionProfiles(openXrAsm, settingsType);

                _loader = ScriptableObject.CreateInstance(_loaderType);
                if (_loader == null)
                {
                    _log.Error("[XR] could not create OpenXRLoader instance.");
                    return;
                }

                if (!InvokeBool(_loader, "Initialize"))
                {
                    _log.Error("[XR] OpenXRLoader.Initialize() returned false. Common causes: " +
                               "native UnityOpenXR files not installed in (Game)_Data (see doc), " +
                               "no OpenXR runtime active (start SteamVR / Meta app), headset asleep.");
                    return;
                }
                _log.Info("[XR] Initialize OK.");

                if (!InvokeBool(_loader, "Start"))
                {
                    _log.Error("[XR] OpenXRLoader.Start() returned false.");
                    return;
                }

                Running = true;
                ApplyResolutionScale();
                WireXrManagement(mgmtAsm);
                ApplyPhysicsRate();
                _log.Info("[XR] OpenXR RUNNING — display + input subsystems started. " +
                          "If the headset still shows nothing, check that a camera exists " +
                          "(spawn the BIMOS rig) and the game camera is disabled.");
            }
            catch (Exception ex)
            {
                _log.Error($"[XR] bootstrap failed: {ex}");
            }
        }

        public void Stop()
        {
            if (!Running || _loader == null || _loaderType == null)
                return;
            try
            {
                InvokeBool(_loader, "Stop");
                InvokeBool(_loader, "Deinitialize");
                Running = false;
                _log.Info("[XR] stopped.");
            }
            catch (Exception ex)
            {
                _log.Error($"[XR] stop failed: {ex}");
            }
        }

        private void EnsureSettingsInstance(Type settingsType)
        {
            var instanceProp = settingsType.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);
            object? current = null;
            try
            {
                current = instanceProp?.GetValue(null, null);
            }
            catch (Exception ex) when (ex.ToString().Contains("UnityEditor"))
            {
                _log.Error("[XR] Your Unity.XR.* DLLs are EDITOR-compiled (they reference " +
                           "UnityEditor). Replace the ones in UserLibs with the copies from a " +
                           "player build's <build>_Data/Managed/ — see docs/PHASE-XR.md.");
                throw;
            }
            if (current != null)
            {
                _log.Info("[XR] OpenXRSettings.Instance already present.");
                return;
            }

            var created = ScriptableObject.CreateInstance(settingsType);
            int planted = 0;
            foreach (var f in settingsType.GetFields(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType == settingsType && f.GetValue(null) == null)
                {
                    f.SetValue(null, created);
                    planted++;
                    _log.Info($"[XR] planted OpenXRSettings into static field '{f.Name}'.");
                }
            }
            if (planted == 0)
                _log.Warn("[XR] found no empty static OpenXRSettings field to fill — " +
                          "continuing anyway (some versions lazily create it).");
        }

        private void ApplyRenderMode(Type settingsType)
        {
            try
            {
                var instanceProp = settingsType.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static);
                var settings = instanceProp?.GetValue(null, null);
                if (settings == null)
                {
                    _log.Warn("[XR] no OpenXRSettings instance — cannot set render mode.");
                    return;
                }

                var modeProp = settingsType.GetProperty("renderMode",
                    BindingFlags.Public | BindingFlags.Instance);
                if (modeProp == null)
                {
                    _log.Warn("[XR] OpenXRSettings.renderMode not found on this package version.");
                    return;
                }

                var value = Enum.Parse(modeProp.PropertyType, RenderMode, ignoreCase: true);
                modeProp.SetValue(settings, value, null);
                _log.Info($"[XR] render mode = {modeProp.GetValue(settings, null)} " +
                          "(MultiPass is the safe choice for games built without XR).");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR] could not set render mode '{RenderMode}': {ex.Message}");
            }
        }

        private void EnableInteractionProfiles(Assembly openXrAsm, Type settingsType)
        {
            try
            {
                var featureBase = openXrAsm.GetType("UnityEngine.XR.OpenXR.Features.OpenXRFeature");
                if (featureBase == null)
                {
                    _log.Warn("[XR] OpenXRFeature type not found — skipping interaction profiles.");
                    return;
                }

                var instanceProp = settingsType.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static);
                var settings = instanceProp?.GetValue(null, null);
                if (settings == null)
                    return;

                var featuresField = settingsType.GetField("features",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (featuresField == null)
                {
                    _log.Warn("[XR] OpenXRSettings.features field not found — " +
                              "controllers may not work on this package version.");
                    return;
                }

                var wanted = new System.Collections.Generic.List<object>();
                var names = new System.Collections.Generic.List<string>();
                bool auto = string.IsNullOrEmpty(InteractionProfiles) ||
                            InteractionProfiles.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase);
                var filters = auto ? new string[0] : InteractionProfiles.Split(',');

                foreach (var type in SafeGetTypes(openXrAsm))
                {
                    bool usable;
                    try
                    {
                        usable = !type.IsAbstract && featureBase.IsAssignableFrom(type);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"[XR] skipping type '{type.Name}': {ex.Message}");
                        continue;
                    }
                    if (!usable)
                        continue;

                    if (type.Name.IndexOf("Profile", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (!auto)
                    {
                        bool match = false;
                        foreach (var f in filters)
                        {
                            var needle = f.Trim();
                            if (needle.Length > 0 &&
                                type.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                match = true;
                                break;
                            }
                        }
                        if (!match) continue;
                    }

                    var feature = ScriptableObject.CreateInstance(type);
                    if (feature == null) continue;

                    var enabledProp = type.GetProperty("enabled",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (enabledProp != null && enabledProp.CanWrite)
                        enabledProp.SetValue(feature, true, null);

                    wanted.Add(feature);
                    names.Add(type.Name);
                }

                if (wanted.Count == 0)
                {
                    _log.Warn($"[XR] no interaction profiles matched '{InteractionProfiles}'.");
                    return;
                }

                var arr = Array.CreateInstance(featureBase, wanted.Count);
                for (int i = 0; i < wanted.Count; i++)
                    arr.SetValue(wanted[i], i);
                featuresField.SetValue(settings, arr);

                _log.Info($"[XR] enabled {wanted.Count} interaction profile(s): {string.Join(", ", names.ToArray())}");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR] interaction profile setup failed: {ex.Message}");
            }
        }

        private void WireXrManagement(Assembly mgmtAsm)
        {
            try
            {
                var generalType = mgmtAsm.GetType("UnityEngine.XR.Management.XRGeneralSettings");
                var managerType = mgmtAsm.GetType("UnityEngine.XR.Management.XRManagerSettings");
                if (generalType == null || managerType == null)
                    return;

                var instanceProp = generalType.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static);
                if (instanceProp?.GetValue(null, null) != null)
                {
                    _log.Info("[XR] XRGeneralSettings.Instance already set.");
                    return;
                }

                var manager = ScriptableObject.CreateInstance(managerType);
                var general = ScriptableObject.CreateInstance(generalType);

                var loadersProp = managerType.GetProperty("loaders", BindingFlags.Public | BindingFlags.Instance);
                if (loadersProp?.GetValue(manager, null) is System.Collections.IList loaders)
                    loaders.Add(_loader);
                SetFieldLike(managerType, manager, "activeLoader", _loader);

                SetFieldLike(generalType, general, "Manager", manager);
                SetFieldLike(generalType, general, "manager", manager);

                if (instanceProp != null && instanceProp.CanWrite)
                    instanceProp.SetValue(null, general, null);
                else
                    SetStaticFieldOfType(generalType, general);

                var check = instanceProp?.GetValue(null, null);
                _log.Info($"[XR] XR Management wired (Instance={(check != null ? "ok" : "FAILED")}) — " +
                          "lets BIMOS's AutoPhysicsRate find the display subsystem.");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR] could not wire XR Management: {ex.Message}");
            }
        }

        private static void SetFieldLike(Type type, object target, string name, object? value)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value, null);
                return;
            }
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (string.Equals(f.Name.TrimStart('m', '_'), name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Name, "m_" + name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (value == null || f.FieldType.IsInstanceOfType(value))
                    {
                        f.SetValue(target, value);
                        return;
                    }
                }
            }
        }

        private static void SetStaticFieldOfType(Type type, object value)
        {
            foreach (var f in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType == type)
                {
                    f.SetValue(null, value);
                    return;
                }
            }
        }

        private void ApplyPhysicsRate()
        {
            if (PhysicsRate < 0f)
                return;

            try
            {
                float rate = PhysicsRate;
                if (rate == 0f)
                    rate = DetectRefreshRate();

                var previous = Time.fixedDeltaTime;
                Time.fixedDeltaTime = 1f / rate;
                _log.Info($"[XR] physics rate {1f / previous:0.#} Hz -> {rate:0.#} Hz " +
                          "(physics-driven rigs need this; the host game's rate makes them unstable).");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR] could not set physics rate: {ex.Message}");
            }
        }

        private float DetectRefreshRate()
        {
            try
            {
                var displays = new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
                SubsystemManager.GetInstances(displays);
                foreach (var d in displays)
                {
                    if (d == null || !d.running)
                        continue;
                    if (d.TryGetDisplayRefreshRate(out var hz) && hz > 0f)
                    {
                        _log.Info($"[XR] display subsystem reports {hz:0.#} Hz.");
                        return hz;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR] display refresh query failed: {ex.Message}");
            }

            try
            {
                var legacy = UnityEngine.XR.XRDevice.refreshRate;
                if (legacy > 0f)
                    return legacy;
            }
            catch {  }

            _log.Warn("[XR] headset refresh rate unavailable; defaulting physics to 72 Hz.");
            return 72f;
        }

        private void ApplyResolutionScale()
        {
            if (ResolutionScale <= 0f || Math.Abs(ResolutionScale - 1f) < 0.001f)
                return;
            try
            {
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = ResolutionScale;
                _log.Info($"[XR] eye resolution scale = {ResolutionScale:0.00} (lower = faster).");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR] could not set resolution scale: {ex.Message}");
            }
        }

        public void LogDiagnostics()
        {
            try
            {
                _log.Info($"[XR-diag] XRSettings.enabled={UnityEngine.XR.XRSettings.enabled} " +
                          $"isDeviceActive={UnityEngine.XR.XRSettings.isDeviceActive} " +
                          $"device='{UnityEngine.XR.XRSettings.loadedDeviceName}' " +
                          $"eyeTex={UnityEngine.XR.XRSettings.eyeTextureWidth}x{UnityEngine.XR.XRSettings.eyeTextureHeight} " +
                          $"stereoMode={UnityEngine.XR.XRSettings.stereoRenderingMode}");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR-diag] XRSettings unavailable: {ex.Message}");
            }

            var cams = Camera.allCameras;
            _log.Info($"[XR-diag] {cams.Length} enabled camera(s):");
            foreach (var cam in cams)
            {
                if (cam == null) continue;
                _log.Info($"[XR-diag]   '{cam.name}' depth={cam.depth} target={cam.stereoTargetEye} " +
                          $"targetTexture={(cam.targetTexture != null ? cam.targetTexture.name : "null")} " +
                          $"cullingMask=0x{cam.cullingMask:X} path={PathOf(cam.transform)}");
            }

            LogInputDevices();
        }

        private void LogInputDevices()
        {
            try
            {
                var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevices(devices);
                _log.Info($"[XR-diag] legacy XR InputDevices: {devices.Count}");
                foreach (var d in devices)
                    _log.Info($"[XR-diag]   '{d.name}' chars={d.characteristics} valid={d.isValid}");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR-diag] XR InputDevices unavailable: {ex.Message}");
            }

            try
            {
                var isAsm = FindAssembly("Unity.InputSystem");
                if (isAsm == null)
                {
                    _log.Info("[XR-diag] Unity.InputSystem not loaded.");
                    return;
                }
                var isType = isAsm.GetType("UnityEngine.InputSystem.InputSystem");
                var devicesProp = isType?.GetProperty("devices",
                    BindingFlags.Public | BindingFlags.Static);
                var list = devicesProp?.GetValue(null, null) as System.Collections.IEnumerable;
                if (list == null)
                {
                    _log.Info("[XR-diag] InputSystem.devices unavailable.");
                    return;
                }
                int n = 0;
                foreach (var d in list)
                {
                    _log.Info($"[XR-diag]   InputSystem device: {d}");
                    n++;
                }
                _log.Info($"[XR-diag] InputSystem devices: {n}");
            }
            catch (Exception ex)
            {
                _log.Warn($"[XR-diag] InputSystem enumeration failed: {ex.Message}");
            }
        }

        private static string PathOf(Transform t)
        {
            var s = t.name;
            for (var p = t.parent; p != null; p = p.parent)
                s = p.name + "/" + s;
            return s;
        }

        private bool InvokeBool(object target, string method)
        {
            var m = _loaderType!.GetMethod(method, Type.EmptyTypes);
            if (m == null)
            {
                _log.Error($"[XR] method '{method}' not found on OpenXRLoader.");
                return false;
            }
            var result = m.Invoke(target, null);
            return result is bool b && b;
        }

        private Type[] SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var good = new System.Collections.Generic.List<Type>();
                foreach (var t in ex.Types)
                {
                    if (t != null)
                        good.Add(t);
                }
                _log.Warn($"[XR] {asm.GetName().Name}: {good.Count} type(s) loaded, " +
                          $"{ex.Types.Length - good.Count} failed (missing optional deps) — continuing.");
                return good.ToArray();
            }
        }

        private static Assembly? FindAssembly(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                        return asm;
                }
                catch {  }
            }
            return null;
        }
    }
}
