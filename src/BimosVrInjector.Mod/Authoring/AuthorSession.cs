using System.Collections.Generic;
using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Core.Authoring;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Resolve;
using BimosVrInjector.Mod.Runtime;
using UnityEngine;

namespace BimosVrInjector.Mod.Authoring
{
    internal sealed class AuthorSession
    {
        private readonly UnitySceneAccess _scene;
        private readonly ILiveRigSpawner _rig;
        private readonly ILog _log;

        public OperationLog Log { get; private set; }

        public UnityTreeNode? BrowseParent { get; private set; }

        public UnityTreeNode? Selected { get; private set; }

        public string SceneName => _scene.ActiveSceneName;

        public AuthorSession(UnitySceneAccess scene, ILiveRigSpawner rig, ILog log)
        {
            _scene = scene;
            _rig = rig;
            _log = log;
            Log = new OperationLog(scene.ActiveSceneName);
        }

        public void OnSceneChanged()
        {
            _scene.Refresh();
            Log = new OperationLog(_scene.ActiveSceneName);
            BrowseParent = null;
            Selected = null;

            var existing = ConfigStore.LoadForScene(ModPaths.ConfigDir, _scene.ActiveSceneName);
            if (existing != null)
            {
                Log.LoadFrom(existing);
                _log.Info($"Resumed existing config for '{_scene.ActiveSceneName}'.");
            }
        }

        public IList<ITreeNode> CurrentChildren =>
            BrowseParent == null ? _scene.Roots : BrowseParent.Children;

        public void Select(UnityTreeNode node) => Selected = node;

        public void EnterSelected()
        {
            if (Selected != null)
            {
                BrowseParent = Selected;
                Selected = null;
            }
        }

        public void GoUp()
        {
            BrowseParent = BrowseParent?.Parent as UnityTreeNode;
            Selected = null;
        }

        public void RefreshScene()
        {
            _scene.Refresh();
            BrowseParent = null;
            Selected = null;
        }

        public bool ToggleDisableSelected()
        {
            if (Selected == null) return false;
            var key = ObjectKey.From(Selected);
            bool disabled = Log.ToggleDisable(key);

            _scene.SetActive(Selected, !disabled);
            return disabled;
        }

        public bool ToggleDeleteSelected()
        {
            if (Selected == null) return false;
            var key = ObjectKey.From(Selected);
            return Log.ToggleDelete(key);
        }

        public bool ToggleGrabbableSelected()
        {
            if (Selected == null) return false;
            var key = ObjectKey.From(Selected);
            return Log.ToggleGrabbable(key);
        }

        public bool SpawnRigAtCamera()
        {
            var cam = CameraUtil.GetActiveCamera();
            if (cam == null)
            {
                _log.Warn("No camera found to align the rig to.");
                return false;
            }

            var t = cam.transform;
            var pos = new[] { t.position.x, t.position.y, t.position.z };
            var euler = t.rotation.eulerAngles;
            var rot = new[] { euler.x, euler.y, euler.z };
            var scale = new[] { 1f, 1f, 1f };

            Log.SetRig(pos, rot, scale);
            _rig.DespawnExisting();
            _rig.Spawn(pos, rot, scale);
            return true;
        }

        public void NudgeRig(Vector3 deltaPos, Vector3 deltaEuler)
        {
            var go = _rig.Current;
            if (go == null)
            {
                _log.Warn("No rig preview to nudge — spawn one first.");
                return;
            }
            go.transform.position += deltaPos;
            go.transform.rotation *= Quaternion.Euler(deltaEuler);

            var p = go.transform.position;
            var e = go.transform.rotation.eulerAngles;
            var s = go.transform.localScale;
            Log.SetRig(new[] { p.x, p.y, p.z }, new[] { e.x, e.y, e.z }, new[] { s.x, s.y, s.z });
        }

        public UnityTreeNode? PickUnderRay(Ray ray)
        {
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var node = UnityTreeNode.Wrap(hit.collider.gameObject);
                Selected = node;
                _log.Info($"Picked '{ObjectKey.From(node)}'.");
                return node;
            }
            _log.Info("Pick ray hit nothing.");
            return null;
        }

        public string Save()
        {
            var cfg = Log.Build();
            var path = ConfigStore.Save(ModPaths.ConfigDir, cfg);
            _log.Info($"Saved config -> {path}");
            return path;
        }
    }
}
