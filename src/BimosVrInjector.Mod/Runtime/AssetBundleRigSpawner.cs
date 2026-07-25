using System.IO;
using BimosVrInjector.Core.Abstractions;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class AssetBundleRigSpawner : ILiveRigSpawner
    {
        public const string RigName = "BIMOS_Rig";

        private readonly string _bundlePath;
        private readonly string _prefabName;
        private readonly ILog _log;
        private readonly CubeRigSpawner _fallback = new CubeRigSpawner();

        public bool ForceVrMode { get; set; } = true;

        public bool IsolateOnSpawn { get; set; } = true;

        private AssetBundle? _bundle;
        private GameObject? _prefab;
        private bool _warnedMissing;
        private GameObject? _current;

        public AssetBundleRigSpawner(string bundlePath, string prefabName, ILog log)
        {
            _bundlePath = bundlePath;
            _prefabName = prefabName ?? "";
            _log = log;
        }

        public GameObject? Current => _current != null ? _current : _fallback.Current;

        public void Spawn(float[] pos, float[] rotEuler, float[] scale)
        {
            var prefab = LoadPrefab();
            if (prefab == null)
            {
                _fallback.Spawn(pos, rotEuler, scale);
                return;
            }

            bool prefabWasActive = prefab.activeSelf;
            if (prefabWasActive)
                prefab.SetActive(false);

            GameObject go;
            try
            {
                go = Object.Instantiate(prefab);
            }
            finally
            {
                if (prefabWasActive)
                    prefab.SetActive(true);
            }

            go.name = RigName;
            go.transform.position = new Vector3(pos[0], pos[1], pos[2]);
            go.transform.rotation = Quaternion.Euler(rotEuler[0], rotEuler[1], rotEuler[2]);
            go.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

            BimosRigInitializer.PrepareInstance(go, _log);
            go.SetActive(true);
            _current = go;

            if (ForceVrMode)
                BimosRigInitializer.ForceVrMode(_log);

            RigOverlapDiagnostics.Report(go, _log, IsolateOnSpawn);
            _log.Info($"Instantiated BIMOS rig '{prefab.name}' at " +
                      $"({pos[0]:0.##}, {pos[1]:0.##}, {pos[2]:0.##}).");
        }

        public void DespawnExisting()
        {
            if (_current != null)
            {
                Object.Destroy(_current);
                _current = null;
            }
            _fallback.DespawnExisting();

            var stray = GameObject.Find(RigName);
            if (stray != null)
                Object.Destroy(stray);
        }

        private void WarnOnBundleUnityVersion()
        {
            try
            {
                var header = new byte[128];
                int read;
                using (var fs = File.OpenRead(_bundlePath))
                    read = fs.Read(header, 0, header.Length);

                var parts = System.Text.Encoding.ASCII.GetString(header, 0, read).Split('\0');
                string built = "";
                foreach (var p in parts)
                {
                    if (p.Length >= 6 && char.IsDigit(p[0]) && p.Contains(".") &&
                        (p.Contains("f") || p.Contains("b") || p.Contains("a")))
                    {
                        built = p;
                        break;
                    }
                }

                var running = Application.unityVersion;
                if (built.Length == 0)
                    return;

                if (built == running)
                {
                    _log.Info($"[bundle] built with Unity {built} (exact match).");
                    return;
                }

                if (CompareUnityVersions(built, running) > 0)
                {
                    _log.Warn($"[bundle] built with Unity {built} but this game runs {running}. " +
                              "A NEWER bundle in an OLDER player is the unsupported direction and can " +
                              "crash natively during deserialization. Build the bundle with " +
                              $"{running} (Unity Hub archive) if you hit unexplained crashes.");
                }
                else
                {
                    _log.Info($"[bundle] built with Unity {built}, running {running} " +
                              "(older->newer is the supported direction).");
                }
            }
            catch {  }
        }

        private static int CompareUnityVersions(string a, string b)
        {
            var pa = SplitVersion(a);
            var pb = SplitVersion(b);
            for (int i = 0; i < 3; i++)
            {
                if (pa[i] != pb[i])
                    return pa[i].CompareTo(pb[i]);
            }
            return 0;
        }

        private static int[] SplitVersion(string v)
        {
            var result = new[] { 0, 0, 0 };
            var chunks = v.Split('.');
            for (int i = 0; i < 3 && i < chunks.Length; i++)
            {
                var digits = "";
                foreach (var c in chunks[i])
                {
                    if (char.IsDigit(c)) digits += c;
                    else break;
                }
                int.TryParse(digits, out result[i]);
            }
            return result;
        }

        private GameObject? LoadPrefab()
        {
            if (_prefab != null)
                return _prefab;

            if (!File.Exists(_bundlePath))
            {
                if (!_warnedMissing)
                {
                    _log.Warn($"No rig bundle at '{_bundlePath}' — using the placeholder cube. " +
                              "Drop a rig.bundle there (see docs/PHASE3-bimos-rig.md) to spawn BIMOS.");
                    _warnedMissing = true;
                }
                return null;
            }

            if (_bundle == null)
            {
                WarnOnBundleUnityVersion();
                _bundle = AssetBundle.LoadFromFile(_bundlePath);
                if (_bundle == null)
                {
                    _log.Error($"Failed to load AssetBundle '{_bundlePath}'. " +
                               "Was it built for this Unity version and StandaloneWindows64?");
                    return null;
                }
            }

            if (_prefabName.Length > 0)
                _prefab = _bundle.LoadAsset<GameObject>(_prefabName);

            if (_prefab == null)
            {
                var all = _bundle.LoadAllAssets<GameObject>();
                if (all != null && all.Length > 0)
                {
                    _prefab = all[0];
                    _log.Warn($"Using first GameObject in bundle: '{_prefab.name}'" +
                              (_prefabName.Length > 0 ? $" (couldn't find '{_prefabName}')." : "."));
                }
                else
                {
                    _log.Error("No GameObject found in bundle. Asset names: " +
                               string.Join(", ", _bundle.GetAllAssetNames()));
                }
            }

            return _prefab;
        }
    }
}
