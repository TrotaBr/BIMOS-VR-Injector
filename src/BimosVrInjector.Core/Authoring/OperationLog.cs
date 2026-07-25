using System;
using System.Collections.Generic;
using BimosVrInjector.Core.Config;

namespace BimosVrInjector.Core.Authoring
{
    public sealed class OperationLog
    {
        public string SceneName { get; private set; }

        private readonly Dictionary<string, ObjectKey> _disable = new Dictionary<string, ObjectKey>();
        private readonly Dictionary<string, ObjectKey> _delete = new Dictionary<string, ObjectKey>();
        private readonly Dictionary<string, GrabbableEntry> _grabbable = new Dictionary<string, GrabbableEntry>();
        private RigSpawn? _rig;

        private readonly List<string> _journal = new List<string>();
        public IList<string> Journal => _journal;

        public OperationLog(string sceneName)
        {
            SceneName = sceneName ?? "";
        }

        public int DisableCount => _disable.Count;
        public int DeleteCount => _delete.Count;
        public int GrabbableCount => _grabbable.Count;
        public bool HasRig => _rig != null;

        public bool IsDisabled(string path) => _disable.ContainsKey(path);
        public bool IsDeleted(string path) => _delete.ContainsKey(path);
        public bool IsGrabbable(string path) => _grabbable.ContainsKey(path);

        public bool ToggleDisable(ObjectKey key)
        {
            if (_disable.Remove(key.Path))
            {
                Note($"un-disabled {key}");
                return false;
            }
            _disable[key.Path] = key;
            _delete.Remove(key.Path);
            Note($"disabled {key}");
            return true;
        }

        public bool ToggleDelete(ObjectKey key)
        {
            if (_delete.Remove(key.Path))
            {
                Note($"un-marked delete {key}");
                return false;
            }
            _delete[key.Path] = key;
            _disable.Remove(key.Path);
            Note($"marked for deletion {key}");
            return true;
        }

        public bool ToggleGrabbable(ObjectKey key)
        {
            if (_grabbable.Remove(key.Path))
            {
                Note($"un-tagged grabbable {key}");
                return false;
            }
            _grabbable[key.Path] = new GrabbableEntry { Target = key };
            Note($"tagged grabbable {key}");
            return true;
        }

        public void SetRig(float[] pos, float[] rot, float[] scale)
        {
            _rig = new RigSpawn { Pos = pos, Rot = rot, Scale = scale };
            Note($"rig at ({pos[0]:0.##}, {pos[1]:0.##}, {pos[2]:0.##})");
        }

        public void ClearRig()
        {
            _rig = null;
            Note("cleared rig");
        }

        public RigSpawn? Rig => _rig;

        public bool AutoGrabAllBodies { get; private set; }

        public bool ToggleAutoGrabAll()
        {
            AutoGrabAllBodies = !AutoGrabAllBodies;
            Note(AutoGrabAllBodies
                ? "auto-grab ALL physics bodies: ON"
                : "auto-grab ALL physics bodies: off");
            return AutoGrabAllBodies;
        }

        public SceneConfig Build()
        {
            var cfg = new SceneConfig
            {
                SceneName = SceneName,
                RigSpawn = _rig,
                AutoGrabAllBodies = AutoGrabAllBodies,
            };
            cfg.Disable.AddRange(_disable.Values);
            cfg.Delete.AddRange(_delete.Values);
            cfg.Grabbable.AddRange(_grabbable.Values);
            return cfg;
        }

        public void LoadFrom(SceneConfig cfg)
        {
            _disable.Clear();
            _delete.Clear();
            _grabbable.Clear();
            SceneName = cfg.SceneName;

            foreach (var k in cfg.Disable) _disable[k.Path] = k;
            foreach (var k in cfg.Delete) _delete[k.Path] = k;
            foreach (var g in cfg.Grabbable) _grabbable[g.Target.Path] = g;
            _rig = cfg.RigSpawn;
            AutoGrabAllBodies = cfg.AutoGrabAllBodies;

            Note($"loaded config ({_disable.Count} disable, {_delete.Count} delete, " +
                 $"{_grabbable.Count} grabbable, rig={( _rig != null )})");
        }

        private void Note(string msg)
        {
            _journal.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (_journal.Count > 200)
                _journal.RemoveAt(0);
        }
    }
}
