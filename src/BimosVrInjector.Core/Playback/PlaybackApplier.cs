using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Resolve;

namespace BimosVrInjector.Core.Playback
{
    public sealed class PlaybackApplier
    {
        private readonly ISceneAccess _scene;
        private readonly IRigSpawner _rig;
        private readonly IGrabTagger _grab;
        private readonly ILog _log;

        public PlaybackApplier(ISceneAccess scene, IRigSpawner rig, IGrabTagger grab, ILog log)
        {
            _scene = scene;
            _rig = rig;
            _grab = grab;
            _log = log;
        }

        public void Apply(SceneConfig config)
        {
            _log.Info($"Applying config for scene '{config.SceneName}': " +
                      $"{config.Disable.Count} disable, {config.Delete.Count} delete, " +
                      $"{config.Grabbable.Count} grabbable, rig={(config.RigSpawn != null)}");

            _scene.Refresh();

            foreach (var key in config.Delete)
            {
                if (TryResolve(key, "delete", out var node))
                    _scene.Destroy(node!);
            }

            _scene.Refresh();

            foreach (var key in config.Disable)
            {
                if (TryResolve(key, "disable", out var node))
                    _scene.SetActive(node!, false);
            }

            if (config.RigSpawn != null)
            {
                _rig.DespawnExisting();
                _rig.Spawn(config.RigSpawn.Pos, config.RigSpawn.Rot, config.RigSpawn.Scale);
                _log.Info("Spawned rig.");
            }

            _scene.Refresh();

            foreach (var entry in config.Grabbable)
            {
                if (TryResolve(entry.Target, "grabbable", out var node))
                    _grab.Tag(node!, entry);
            }

            if (config.AutoGrabAllBodies)
            {
                var count = _grab.AutoTagAllBodies();
                _log.Info($"Auto-grab: tagged {count} physics body/bodies in '{config.SceneName}'.");
            }
        }

        private bool TryResolve(ObjectKey key, string action, out ITreeNode? node)
        {
            var result = ObjectResolver.Resolve(_scene.Roots, key);
            node = result.Node;

            if (!result.Matched)
            {
                _log.Warn($"[{action}] could not resolve '{key}' -> {result.Reason}. Skipping.");
                return false;
            }

            if (result.Confidence < 1.0f)
                _log.Warn($"[{action}] resolved '{key}' by {result.Reason} — verify this is correct.");

            return true;
        }
    }
}
