using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Playback;
using BimosVrInjector.Mod.Runtime;

namespace BimosVrInjector.Mod.Playback
{
    internal sealed class PlaybackController
    {
        private readonly ISceneAccess _scene;
        private readonly IRigSpawner _rig;
        private readonly IGrabTagger _grab;
        private readonly ILog _log;

        public PlaybackController(ISceneAccess scene, IRigSpawner rig, IGrabTagger grab, ILog log)
        {
            _scene = scene;
            _rig = rig;
            _grab = grab;
            _log = log;
        }

        public void Apply(string sceneName)
        {
            SceneConfig? cfg;
            try
            {
                cfg = ConfigStore.LoadForScene(ModPaths.ConfigDir, sceneName);
            }
            catch (System.Exception ex)
            {
                _log.Error($"Failed to read config for scene '{sceneName}': {ex.Message}");
                return;
            }

            if (cfg == null)
            {
                _log.Info($"No config for scene '{sceneName}' — nothing to apply.");
                return;
            }

            new PlaybackApplier(_scene, _rig, _grab, _log).Apply(cfg);
        }
    }
}
