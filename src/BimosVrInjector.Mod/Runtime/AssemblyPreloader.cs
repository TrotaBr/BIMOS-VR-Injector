using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BimosVrInjector.Core.Abstractions;
using MelonLoader.Utils;

namespace BimosVrInjector.Mod.Runtime
{
    internal static class AssemblyPreloader
    {
        public static string Skip { get; set; } = "";

        public static void PreloadUserLibs(ILog log)
        {
            var gameRoot = Path.GetDirectoryName(MelonEnvironment.UserDataDirectory);
            if (gameRoot == null)
                return;
            var dir = Path.Combine(gameRoot, "UserLibs");
            if (!Directory.Exists(dir))
                return;

            var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { loaded.Add(asm.GetName().Name); } catch {  }
            }

            foreach (var file in Directory.GetFiles(dir, "*.dll"))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(file).Name;
                    if (loaded.Contains(name))
                        continue;
                    if (ShouldSkip(name))
                    {
                        log.Info($"Skipped UserLibs assembly (UserLibsSkip): {name}");
                        continue;
                    }
                    Assembly.LoadFrom(file);
                    log.Info($"Preloaded UserLibs assembly: {name}");
                }
                catch (BadImageFormatException)
                {
                }
                catch (Exception ex)
                {
                    log.Warn($"Could not preload '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
        }

        private static bool ShouldSkip(string assemblyName)
        {
            if (string.IsNullOrEmpty(Skip))
                return false;
            foreach (var frag in Skip.Split(';'))
            {
                var f = frag.Trim();
                if (f.Length > 0 &&
                    assemblyName.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
