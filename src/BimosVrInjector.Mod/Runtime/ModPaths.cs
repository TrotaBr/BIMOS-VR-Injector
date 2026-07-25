using System.IO;
using MelonLoader.Utils;

namespace BimosVrInjector.Mod.Runtime
{
    internal static class ModPaths
    {
        public static string DataDir =>
            Path.Combine(MelonEnvironment.UserDataDirectory, "BimosVrInjector");

        public static string ConfigDir => Path.Combine(DataDir, "configs");

        public static string RigBundlePath(string fileName) => Path.Combine(DataDir, fileName);
    }
}
