using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Mod.Authoring;
using BimosVrInjector.Mod.Playback;
using BimosVrInjector.Mod.Runtime;
using BimosVrInjector.Mod.UI;
using MelonLoader;
using UnityEngine;
using UniverseLib;
using UniverseLib.Config;
using UniverseLib.Input;
using UniverseLib.UI;

[assembly: MelonInfo(typeof(BimosVrInjector.Mod.ModEntry), "BimosVrInjector", "0.1.0", "Garebeu")]
[assembly: MelonGame(null, null)]

namespace BimosVrInjector.Mod
{
    public sealed class ModEntry : MelonMod
    {
        private const string UiGuid = "com.modder.bimosvrinjector";

        internal static ModEntry Instance { get; private set; } = null!;

        internal enum Mode { Playback, Author }
        internal Mode CurrentMode { get; private set; } = Mode.Playback;

        internal MelonLog Log { get; private set; } = null!;
        internal AuthorSession Author => _author!;

        private UnitySceneAccess _scene = null!;
        private ILiveRigSpawner _rig = null!;
        private IGrabTagger _grab = null!;
        private PlaybackController _playback = null!;

        private AuthorSession? _author;
        private AuthorPanel? _panel;
        private UIBase? _uiBase;
        private bool _uiReady;
        private bool _panelNeedsRefresh;
        private XrBootstrap _xr = null!;
        private bool _xrAutoStart = true;

        public override void OnInitializeMelon()
        {
            Instance = this;
            Log = new MelonLog(LoggerInstance);

            AssemblyPreloader.Skip = MelonPreferences.CreateCategory("BimosVrInjector")
                .CreateEntry("UserLibsSkip", "").Value;
            AssemblyPreloader.PreloadUserLibs(Log);

            var prefs = MelonPreferences.CreateCategory("BimosVrInjector");
            var bundleFile = prefs.CreateEntry("RigBundleFile", "rig.bundle").Value;
            var prefabName = prefs.CreateEntry("RigPrefabName", "").Value;

            _scene = new UnitySceneAccess();
            _rig = new AssetBundleRigSpawner(ModPaths.RigBundlePath(bundleFile), prefabName, Log)
            {
                ForceVrMode = prefs.CreateEntry("RigForceVrMode", true).Value,
                IsolateOnSpawn = prefs.CreateEntry("RigIsolateOnSpawn", true).Value,
            };
            _grab = new BimosGrabTagger(Log);
            _playback = new PlaybackController(_scene, _rig, _grab, Log);

            _xr = new XrBootstrap(Log)
            {
                RenderMode = prefs.CreateEntry("XrRenderMode", "MultiPass").Value,
                InteractionProfiles = prefs.CreateEntry("XrInteractionProfiles", "auto").Value,
                ResolutionScale = prefs.CreateEntry("XrResolutionScale", 1.0f).Value,

                PhysicsRate = prefs.CreateEntry("XrPhysicsRate", 0f).Value,
            };
            _xrAutoStart = prefs.CreateEntry("XrAutoStartWithRig", true).Value;

            BimosLayerFix.Apply(HarmonyInstance, Log);

            Interop.RegisterInjectedTypes();
            InitUniverseLib();

            Log.Info("BimosVrInjector 0.1.0  Copyright (C) 2026 Garebeu");
            Log.Info("GPL-3.0, no warranty. Source: https://github.com/Garebeu/BimosVrInjector");
            Log.Info("F5 = Author mode, F9 = start OpenXR.");
        }

        private void InitUniverseLib()
        {
            Universe.Init(
                startupDelay: 1f,
                onInitialized: OnUniverseReady,
                logHandler: (msg, type) => {  },
                config: new UniverseLibConfig
                {
                    Force_Unlock_Mouse = true,
                    Disable_EventSystem_Override = false,
                });
        }

        private void OnUniverseReady()
        {
            _uiBase = UniversalUI.RegisterUI(UiGuid, null);
            _uiReady = true;
            Log.Info("UniverseLib UI ready.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                _scene.Refresh();

                if (CurrentMode == Mode.Playback)
                {
                    if (_xrAutoStart && HasRigForScene(sceneName))
                        EnsureXrStarted();

                    _playback.Apply(sceneName);
                }
                else
                {
                    _author?.OnSceneChanged();

                    _panelNeedsRefresh = true;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"OnSceneWasLoaded('{sceneName}') failed: {ex}");
            }
        }

        public override void OnUpdate()
        {
            if (!_uiReady)
                return;

            if (InputManager.GetKeyDown(KeyCode.F5))
                ToggleAuthorMode();

            if (InputManager.GetKeyDown(KeyCode.F9))
            {
                try { _xr.TryStart(); }
                catch (System.Exception ex) { Log.Error($"XR start failed: {ex}"); }
            }

            if (InputManager.GetKeyDown(KeyCode.F10))
            {
                try { _xr.LogDiagnostics(); }
                catch (System.Exception ex) { Log.Error($"XR diagnostics failed: {ex}"); }
            }

            if (InputManager.GetKeyDown(KeyCode.F11))
            {
                try
                {
                    var n = _grab.AutoTagAllBodies();
                    Log.Info($"Auto-grab: tagged {n} physics body/bodies. " +
                             "Enable it permanently with the author panel's 'Auto-grab all' toggle (saves to config).");
                }
                catch (System.Exception ex) { Log.Error($"Auto-grab failed: {ex}"); }
            }

            if (CurrentMode != Mode.Author)
                return;

            if (_panelNeedsRefresh)
            {
                _panelNeedsRefresh = false;
                try { _panel?.RefreshAll(); }
                catch (System.Exception ex) { Log.Error($"panel refresh failed: {ex}"); }
            }

            try
            {
                if (InputManager.GetKeyDown(KeyCode.F6)) _panel?.SpawnRigAtCamera();
                if (InputManager.GetKeyDown(KeyCode.F7)) _panel?.SaveConfig();
                if (InputManager.GetKeyDown(KeyCode.F8)) TestReplayCurrentScene();
                if (InputManager.GetKeyDown(KeyCode.P)) _panel?.PickUnderCursor();
            }
            catch (System.Exception ex)
            {
                Log.Error($"author hotkey action failed: {ex}");
            }
        }

        private void ToggleAuthorMode()
        {
            if (CurrentMode == Mode.Playback)
            {
                CurrentMode = Mode.Author;

                if (_author == null)
                    _author = new AuthorSession(_scene, _rig, Log);
                _author.OnSceneChanged();

                if (_panel == null)
                    _panel = new AuthorPanel(_uiBase!);
                _panel.SetActive(true);
                _panelNeedsRefresh = true;

                Log.Info("Author mode ON. Browse the list, click to select, use the buttons. Save writes the config.");
            }
            else
            {
                CurrentMode = Mode.Playback;
                _panel?.SetActive(false);
                Log.Info("Author mode OFF (playback).");
            }
        }

        internal int AutoTagAllBodies() => _grab.AutoTagAllBodies();

        private bool HasRigForScene(string sceneName)
        {
            try
            {
                var cfg = Core.Config.ConfigStore.LoadForScene(ModPaths.ConfigDir, sceneName);
                return cfg?.RigSpawn != null;
            }
            catch { return false; }
        }

        internal void EnsureXrStarted()
        {
            if (_xr.Running)
                return;
            Log.Info("Starting OpenXR before spawning the rig (tracking + physics rate depend on it)…");
            try { _xr.TryStart(); }
            catch (System.Exception ex) { Log.Error($"XR start failed: {ex}"); }
        }

        internal void TestReplayCurrentScene()
        {
            Log.Info("Test replay: applying saved config for the current scene…");
            _playback.Apply(_scene.ActiveSceneName);
        }
    }
}
