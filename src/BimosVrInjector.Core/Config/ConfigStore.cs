using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BimosVrInjector.Core.Config
{
    public static class ConfigStore
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        public static string Serialize(SceneConfig config)
            => JsonConvert.SerializeObject(config, Settings);

        public static SceneConfig? Deserialize(string json)
            => JsonConvert.DeserializeObject<SceneConfig>(json, Settings);

        public static string PathForScene(string dir, string sceneName)
            => Path.Combine(dir, Sanitize(sceneName) + ".json");

        public static string Save(string dir, SceneConfig config)
        {
            Directory.CreateDirectory(dir);
            var path = PathForScene(dir, config.SceneName);
            File.WriteAllText(path, Serialize(config), new UTF8Encoding(false));
            return path;
        }

        public static SceneConfig? LoadForScene(string dir, string sceneName)
        {
            var path = PathForScene(dir, sceneName);
            if (!File.Exists(path))
                return null;
            return Deserialize(File.ReadAllText(path));
        }

        private static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(System.Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            var cleaned = sb.ToString().Trim();
            return cleaned.Length == 0 ? "_unnamed_scene_" : cleaned;
        }
    }
}
