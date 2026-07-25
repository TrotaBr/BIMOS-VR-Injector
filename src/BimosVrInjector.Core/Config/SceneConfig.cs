using System.Collections.Generic;

namespace BimosVrInjector.Core.Config
{
    public sealed class SceneConfig
    {
        public int FormatVersion { get; set; } = 1;

        public string SceneName { get; set; } = "";

        public List<ObjectKey> Disable { get; set; } = new List<ObjectKey>();

        public List<ObjectKey> Delete { get; set; } = new List<ObjectKey>();

        public List<GrabbableEntry> Grabbable { get; set; } = new List<GrabbableEntry>();

        public RigSpawn? RigSpawn { get; set; }

        public bool AutoGrabAllBodies { get; set; }
    }

    public sealed class GrabbableEntry
    {
        public ObjectKey Target { get; set; } = new ObjectKey();

        public GrabOptions? Grab { get; set; }
    }

    public sealed class GrabOptions
    {
    }

    public sealed class RigSpawn
    {
        public float[] Pos { get; set; } = new float[] { 0f, 0f, 0f };
        public float[] Rot { get; set; } = new float[] { 0f, 0f, 0f };
        public float[] Scale { get; set; } = new float[] { 1f, 1f, 1f };
    }
}
