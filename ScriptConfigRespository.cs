using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Lazy_App_Codex_Core
{
    public sealed class AppSettings
    {
        public string HotkeyStart { get; set; } = "CTRL+ALT+S";
        public string HotkeyStop { get; set; } = "CTRL+ALT+D";
    }

    public class ScriptConfigRepository
    {
        private readonly string _configPath;

        public AppSettings Settings { get; private set; } = new AppSettings();
        public int OffsetX { get; private set; } = 5;
        public int OffsetY { get; private set; } = 5;
        private Dictionary<string, (int x, int y)> _offsetProfiles = new Dictionary<string, (int x, int y)>(StringComparer.OrdinalIgnoreCase);

        public ScriptConfigRepository(string configPath)
        {
            _configPath = configPath;
        }

        public JObject LoadRawConfig()
        {
            EnsureConfigFileExists();

            JObject root;
            string json = File.ReadAllText(_configPath);
            root = JsonConvert.DeserializeObject<JObject>(json) ?? CreateDefaultConfig();

            EnsureConfigCategories(root);
            return root;
        }

        public void SaveRawConfig(JObject root)
        {
            EnsureConfigCategories(root);
            string json = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(_configPath, json);
        }

        public Dictionary<string, ScriptModel> Load()
        {
            EnsureConfigFileExists();

            Settings = new AppSettings();
            OffsetX = 5;
            OffsetY = 5;
            _offsetProfiles = new Dictionary<string, (int x, int y)>(StringComparer.OrdinalIgnoreCase);

            string json = File.ReadAllText(_configPath);
            var root = JsonConvert.DeserializeObject<JObject>(json);
            if (root == null)
            {
                return new Dictionary<string, ScriptModel>();
            }

            EnsureConfigCategories(root);

            var scriptsNode = root["scripts"] as JObject ?? root;
            var parsedSettings = root["settings"]?.ToObject<AppSettings>();
            if (parsedSettings != null)
            {
                Settings = parsedSettings;
            }

            if (root["offset"] is JObject offsetObj)
            {
                OffsetX = ReadIntOrFallback(offsetObj, 5, 0, "offsetX", "ox", "x", "s");
                OffsetY = ReadIntOrFallback(offsetObj, 5, 1, "offsetY", "oy", "y", "s");
                _offsetProfiles = ReadOffsetProfiles(offsetObj, OffsetX, OffsetY);
            }

            var scripts = new Dictionary<string, ScriptModel>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in scriptsNode.Properties())
            {
                if (property.Name.Equals("settings", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("scripts", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("offset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value is not JObject scriptObj)
                {
                    continue;
                }

                scripts[property.Name] = ParseScript(scriptObj);
            }

            return scripts;
        }

        private void EnsureConfigFileExists()
        {
            if (File.Exists(_configPath))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(Path.GetFullPath(_configPath));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            SaveRawConfig(CreateDefaultConfig());
        }

        private static JObject CreateDefaultConfig()
        {
            return new JObject
            {
                ["settings"] = JObject.FromObject(new AppSettings()),
                ["offset"] = new JObject
                {
                    ["s26"] = new JArray(5, 5),
                    ["s13"] = new JArray(5, 5)
                },
                ["scripts"] = new JObject()
            };
        }

        private static void EnsureConfigCategories(JObject root)
        {
            EnsureCategory(root, "settings");
            EnsureCategory(root, "offset");
            EnsureCategory(root, "scripts");
            MigrateSetting(root, "hotkeyStartStopToggle", "hotkeyStart");
            EnsureSetting(root, "hotkeyStart", "CTRL+ALT+S");
            EnsureSetting(root, "hotkeyStop", "CTRL+ALT+D");
        }

        private static void EnsureCategory(JObject root, string name)
        {
            if (root[name] is not JObject)
            {
                root[name] = new JObject();
            }
        }

        private static void EnsureSetting(JObject root, string key, string defaultValue)
        {
            var settings = (JObject)root["settings"]!;
            if (settings[key] == null)
            {
                settings[key] = defaultValue;
            }
        }

        private static void MigrateSetting(JObject root, string oldKey, string newKey)
        {
            var settings = (JObject)root["settings"]!;
            if (settings[newKey] == null && settings[oldKey] != null)
            {
                settings[newKey] = settings[oldKey]!.DeepClone();
            }

            settings.Property(oldKey)?.Remove();
        }

        public int GetOffsetUnitForScript(string scriptName, string axis)
        {
            foreach (Match match in Regex.Matches(scriptName, @"\d+"))
            {
                string profileName = "s" + match.Value;
                if (_offsetProfiles.TryGetValue(profileName, out var profile))
                {
                    return axis.Equals("x", StringComparison.OrdinalIgnoreCase) ? profile.x : profile.y;
                }
            }

            return axis.Equals("x", StringComparison.OrdinalIgnoreCase) ? OffsetX : OffsetY;
        }

        private static Dictionary<string, (int x, int y)> ReadOffsetProfiles(JObject offsetObj, int fallbackX, int fallbackY)
        {
            var profiles = new Dictionary<string, (int x, int y)>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in offsetObj.Properties())
            {
                if (!Regex.IsMatch(property.Name, @"^s\d+$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                int x = TryParseIntToken(property.Value, 0, out var parsedX) ? parsedX : fallbackX;
                int y = TryParseIntToken(property.Value, 1, out var parsedY) ? parsedY : fallbackY;
                profiles[property.Name] = (x, y);
            }

            return profiles;
        }

        private static ScriptModel ParseScript(JObject scriptObj)
        {
            var defaults = scriptObj["defaults"] as JObject;
            var rawSteps = scriptObj["config"] as JArray ?? scriptObj["steps"] as JArray ?? new JArray();
            var expandedSteps = ExpandSteps(rawSteps);

            var normalized = new JObject
            {
                [nameof(ScriptModel.Duration)] = ReadInt(scriptObj, defaults, 0, "duration", "d"),
                [nameof(ScriptModel.Interval_Min)] = ReadInt(scriptObj, defaults, 0, "interval_min", "imin", "interval", "i"),
                [nameof(ScriptModel.Interval_Max)] = ReadInt(scriptObj, defaults, 1, "interval_max", "imax", "interval", "i"),
                [nameof(ScriptModel.Config)] = new JArray(expandedSteps.Select(step => NormalizeStep(step as JObject, defaults)))
            };

            return normalized.ToObject<ScriptModel>() ?? new ScriptModel();
        }

        private static IEnumerable<JToken> ExpandSteps(JArray rawSteps)
        {
            foreach (var item in rawSteps)
            {
                if (item is not JObject stepObj)
                {
                    continue;
                }

                var nested = stepObj["steps"] as JArray;
                int repeat = ReadInt(stepObj, null, -1, "repeat", "rep");

                if (nested == null)
                {
                    yield return stepObj;
                    continue;
                }

                int times = repeat > 0 ? repeat : 1;
                for (int i = 0; i < times; i++)
                {
                    foreach (var nestedStep in ExpandSteps(nested))
                    {
                        yield return nestedStep.DeepClone();
                    }
                }
            }
        }

        private static JObject NormalizeStep(JObject? stepObj, JObject? defaults)
        {
            stepObj ??= new JObject();

            return new JObject
            {
                [nameof(StepAction.Act)] = NormalizeAction(ReadString(stepObj, defaults, "leftclick", "act", "a")),
                [nameof(StepAction.ScrX)] = ReadInt(stepObj, defaults, 0, "scrX", "sx", "x", "posX", "px", "scr", "s", "p"),
                [nameof(StepAction.ScrY)] = ReadInt(stepObj, defaults, 1, "scrY", "sy", "y", "posY", "py", "scr", "s", "p"),
                [nameof(StepAction.ScrX2)] = ReadNullableInt(stepObj, defaults, 0, "scrX2", "sx2", "x2", "posX2", "px2", "scr2", "s2", "p2"),
                [nameof(StepAction.ScrY2)] = ReadNullableInt(stepObj, defaults, 1, "scrY2", "sy2", "y2", "posY2", "py2", "scr2", "s2", "p2"),
                [nameof(StepAction.RandX)] = ReadInt(stepObj, defaults, 0, "randX", "rx", "rand", "r"),
                [nameof(StepAction.RandY)] = ReadInt(stepObj, defaults, 1, "randY", "ry", "rand", "r"),
                [nameof(StepAction.Sleep_Min)] = ReadInt(stepObj, defaults, 0, "sleep_min", "smin", "sleep", "t"),
                [nameof(StepAction.Sleep_Max)] = ReadInt(stepObj, defaults, 1, "sleep_max", "smax", "sleep", "t"),
                [nameof(StepAction.Offset)] = ReadString(stepObj, defaults, "", "offset", "o")
            };
        }

        private static string ReadString(JObject source, JObject? defaults, string fallback, params string[] aliases)
        {
            var token = TryGetToken(source, defaults, aliases);
            return string.IsNullOrWhiteSpace(token?.ToString()) ? fallback : token!.ToString();
        }

        private static string NormalizeAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return "leftclick";
            }

            return action.Trim().ToLowerInvariant() switch
            {
                "left" => "leftclick",
                "right" => "rightclick",
                "drag" => "drag",
                _ => action.Trim().ToLowerInvariant()
            };
        }


        private static int ReadInt(JObject source, JObject? defaults, int index, params string[] aliases)
        {
            var token = TryGetToken(source, defaults, aliases);
            return TryParseIntToken(token, index, out var value) ? value : 0;
        }

        private static int ReadIntOrFallback(JObject source, int fallback, int index, params string[] aliases)
        {
            var token = TryGetToken(source, null, aliases);
            return TryParseIntToken(token, index, out var value) ? value : fallback;
        }

        private static int? ReadNullableInt(JObject source, JObject? defaults, int index, params string[] aliases)
        {
            var token = TryGetToken(source, defaults, aliases);
            return TryParseIntToken(token, index, out var value) ? value : null;
        }

        private static JToken? TryGetToken(JObject source, JObject? defaults, params string[] aliases)
        {
            foreach (var key in aliases)
            {
                var sourceToken = source.GetValue(key, StringComparison.OrdinalIgnoreCase);
                if (sourceToken != null)
                {
                    return sourceToken;
                }

                var defaultToken = defaults?.GetValue(key, StringComparison.OrdinalIgnoreCase);
                if (defaultToken != null)
                {
                    return defaultToken;
                }
            }

            return null;
        }

        private static bool TryParseIntToken(JToken? value, int index, out int parsed)
        {
            parsed = 0;
            if (value == null)
            {
                return false;
            }

            if (index >= 0 && value is JArray array)
            {
                if (array.Count <= index)
                {
                    return false;
                }

                value = array[index];
            }

            return int.TryParse(value.ToString(), out parsed);
        }
    }
}
