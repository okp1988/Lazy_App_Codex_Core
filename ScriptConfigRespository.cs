using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Lazy_App_Codex_Core
{
    public sealed class AppSettings
    {
        [JsonProperty("hotkeyStart")]
        public string HotkeyStart { get; set; } = "CTRL+ALT+S";

        [JsonProperty("hotkeyStop")]
        public string HotkeyStop { get; set; } = "CTRL+ALT+D";

        [JsonProperty("hotkeyBackupStart")]
        public string HotkeyBackupStart { get; set; } = "";

        [JsonProperty("hotkeyBackupStop")]
        public string HotkeyBackupStop { get; set; } = "";

        [JsonProperty("tag")]
        public List<string> Tags { get; set; } = new();

        [JsonProperty("devices")]
        public Dictionary<string, DeviceInfo> Devices { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class DeviceInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; } = "";

        [JsonProperty("model")]
        public string Model { get; set; } = "";

        [JsonProperty("lastSerial")]
        public string LastSerial { get; set; } = "";

        [JsonProperty("lastSeen")]
        public string LastSeen { get; set; } = "";
    }

    public sealed class ScriptConfigRepository
    {
        private readonly string _configPath;
        private Dictionary<string, (int x, int y)> _offsetProfiles = new(StringComparer.OrdinalIgnoreCase);

        public ScriptConfigRepository(string configPath)
        {
            _configPath = configPath;
        }

        public string ConfigPath => Path.GetFullPath(_configPath);
        public string ConfigFolder => Path.GetDirectoryName(ConfigPath) ?? Environment.CurrentDirectory;
        public AppSettings Settings { get; private set; } = new();
        public int OffsetX { get; private set; } = 5;
        public int OffsetY { get; private set; } = 5;

        public JObject LoadRawConfig()
        {
            EnsureConfigFileExists();
            var root = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_configPath)) ?? CreateDefaultConfig();
            EnsureConfigCategories(root);
            MigrateConfig(root);
            return root;
        }

        public void SaveRawConfig(JObject root)
        {
            EnsureConfigCategories(root);
            MigrateConfig(root);
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(root, Formatting.Indented));
        }

        public Dictionary<string, ScriptModel> Load()
        {
            return LoadLibrary().Scripts.ToDictionary(script => script.Name, StringComparer.OrdinalIgnoreCase);
        }

        public ConfigLibrary LoadLibrary()
        {
            var root = LoadRawConfig();
            Settings = root["settings"]?.ToObject<AppSettings>() ?? new AppSettings();
            Settings.Devices ??= new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);
            LoadOffsets(root);

            var library = new ConfigLibrary();
            var scripts = (JObject)root["scripts"]!;
            int fallbackOrder = 0;
            foreach (var property in scripts.Properties())
            {
                if (property.Value is JObject scriptObj)
                {
                    library.Scripts.Add(ParseScript(property.Name, scriptObj, fallbackOrder++));
                }
            }

            var sequences = (JObject)root["sequences"]!;
            fallbackOrder = 0;
            foreach (var property in sequences.Properties())
            {
                if (property.Value is JObject sequenceObj)
                {
                    library.Sequences.Add(ParseSequence(property.Name, sequenceObj, fallbackOrder++));
                }
            }

            library.Scripts = library.Scripts.OrderBy(script => script.Order).ThenBy(script => script.Name).ToList();
            library.Sequences = library.Sequences.OrderBy(sequence => sequence.Order).ThenBy(sequence => sequence.Name).ToList();
            return library;
        }

        public void BackupConfig()
        {
            EnsureConfigFileExists();
            string backupFolder = Path.Combine(ConfigFolder, "backup");
            Directory.CreateDirectory(backupFolder);
            string filename = "config_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            File.Copy(ConfigPath, Path.Combine(backupFolder, filename), overwrite: false);
        }

        public void RestoreConfig(string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Backup JSON was not found.", backupPath);
            }

            var root = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(backupPath));
            if (root == null)
            {
                throw new InvalidOperationException("Backup JSON is not valid.");
            }

            EnsureConfigCategories(root);
            MigrateConfig(root);
            SaveRawConfig(root);
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

        private void EnsureConfigFileExists()
        {
            if (File.Exists(_configPath))
            {
                return;
            }

            Directory.CreateDirectory(ConfigFolder);
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
                ["scripts"] = new JObject(),
                ["sequences"] = new JObject()
            };
        }

        private static void EnsureConfigCategories(JObject root)
        {
            EnsureCategory(root, "settings");
            EnsureCategory(root, "offset");
            EnsureCategory(root, "scripts");
            EnsureCategory(root, "sequences");
            MigrateSetting(root, "hotkeyStartStopToggle", "hotkeyStart");
            NormalizeSettingKey(root, "HotkeyStart", "hotkeyStart");
            NormalizeSettingKey(root, "HotkeyStop", "hotkeyStop");
            NormalizeSettingKey(root, "HotkeyBackupStart", "hotkeyBackupStart");
            NormalizeSettingKey(root, "HotkeyBackupStop", "hotkeyBackupStop");
            EnsureSetting(root, "hotkeyStart", "CTRL+ALT+S");
            EnsureSetting(root, "hotkeyStop", "CTRL+ALT+D");
            EnsureSetting(root, "hotkeyBackupStart", "");
            EnsureSetting(root, "hotkeyBackupStop", "");
            EnsureTagSettings(root);
            EnsureDeviceSettings(root);
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
            settings[key] ??= defaultValue;
        }

        private static void EnsureDeviceSettings(JObject root)
        {
            var settings = (JObject)root["settings"]!;
            if (settings["devices"] is not JObject)
            {
                settings["devices"] = new JObject();
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

        private static void NormalizeSettingKey(JObject root, string oldKey, string newKey)
        {
            var settings = (JObject)root["settings"]!;
            if (settings[newKey] == null && settings[oldKey] != null)
            {
                settings[newKey] = settings[oldKey]!.DeepClone();
            }

            settings.Property(oldKey)?.Remove();
        }

        private static void MigrateConfig(JObject root)
        {
            var tags = ReadSettingsTags(root);
            var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
            var scripts = (JObject)root["scripts"]!;
            int order = 0;
            foreach (var property in scripts.Properties().ToList())
            {
                if (property.Value is not JObject script)
                {
                    continue;
                }

                script["id"] ??= NewId("scr");
                script["name"] ??= property.Name;
                script["order"] ??= order;
                if (script["tag"] == null || (script["tag"]!.ToString().Length > 0 && !tagSet.Contains(script["tag"]!.ToString())))
                {
                    script["tag"] = "";
                }
                script["hide"] ??= false;
                script["emin"] ??= 0;
                script["defaultOffsetEnabled"] ??= false;
                script["defaultOffset"] ??= "0";
                if (script["config"] is not JArray)
                {
                    script["config"] = script["steps"] is JArray legacySteps ? legacySteps.DeepClone() : new JArray();
                }

                order++;
            }

            var sequences = (JObject)root["sequences"]!;
            order = 0;
            foreach (var property in sequences.Properties().ToList())
            {
                if (property.Value is not JObject sequence)
                {
                    continue;
                }

                sequence["id"] ??= NewId("seq");
                sequence["name"] ??= property.Name;
                sequence["order"] ??= order++;
                if (sequence["tag"] == null || (sequence["tag"]!.ToString().Length > 0 && !tagSet.Contains(sequence["tag"]!.ToString())))
                {
                    sequence["tag"] = "";
                }
                sequence["d"] ??= 1;
                sequence["imin"] ??= 0;
                sequence["imax"] ??= 0;
                sequence["emin"] ??= 0;
                sequence["defaultOffsetEnabled"] ??= false;
                sequence["defaultOffset"] ??= "0";
                sequence["items"] ??= new JArray();
            }
        }

        private static void EnsureTagSettings(JObject root)
        {
            var settings = (JObject)root["settings"]!;
            if (settings["tag"] is JArray existingTags)
            {
                var normalized = NormalizeTags(existingTags.Select(tag => tag?.ToString() ?? ""));
                settings["tag"] = new JArray(normalized);
                return;
            }

            if (settings["tags"] is JArray legacyTags)
            {
                settings["tag"] = new JArray(NormalizeTags(legacyTags.Select(tag => tag?.ToString() ?? "")));
                settings.Property("tags")?.Remove();
                return;
            }

            settings["tag"] = new JArray();
        }

        private static List<string> ReadSettingsTags(JObject root)
        {
            var settings = (JObject)root["settings"]!;
            if (settings["tag"] is JArray tags)
            {
                return NormalizeTags(tags.Select(tag => tag?.ToString() ?? ""));
            }

            return new List<string>();
        }

        private static List<string> NormalizeTags(IEnumerable<string> tags)
        {
            var normalized = new List<string>();
            foreach (string tag in tags)
            {
                string value = tag.Trim();
                if (value.Length == 0 ||
                    value.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                normalized.Add(value);
            }

            return normalized;
        }

        private void LoadOffsets(JObject root)
        {
            OffsetX = 5;
            OffsetY = 5;
            _offsetProfiles = new Dictionary<string, (int x, int y)>(StringComparer.OrdinalIgnoreCase);
            if (root["offset"] is not JObject offsetObj)
            {
                return;
            }

            OffsetX = ReadIntOrFallback(offsetObj, 5, 0, "offsetX", "ox", "x", "s");
            OffsetY = ReadIntOrFallback(offsetObj, 5, 1, "offsetY", "oy", "y", "s");
            foreach (var property in offsetObj.Properties())
            {
                if (!Regex.IsMatch(property.Name, @"^s\d+$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                int x = TryParseIntToken(property.Value, 0, out var parsedX) ? parsedX : OffsetX;
                int y = TryParseIntToken(property.Value, 1, out var parsedY) ? parsedY : OffsetY;
                _offsetProfiles[property.Name] = (x, y);
            }
        }

        private static ScriptModel ParseScript(string key, JObject scriptObj, int fallbackOrder)
        {
            var script = new ScriptModel
            {
                Id = ReadString(scriptObj, null, NewId("scr"), "id"),
                Name = ReadString(scriptObj, null, key, "name"),
                Tag = ReadString(scriptObj, null, "", "tag"),
                Hidden = ReadBool(scriptObj, false, "hide", "hidden"),
                Order = ReadInt(scriptObj, null, fallbackOrder, -1, "order"),
                Duration = ReadInt(scriptObj, scriptObj["defaults"] as JObject, 0, -1, "duration", "d"),
                Interval_Min = ReadInt(scriptObj, scriptObj["defaults"] as JObject, 0, 0, "interval_min", "imin", "interval", "i"),
                Interval_Max = ReadInt(scriptObj, scriptObj["defaults"] as JObject, 1, 1, "interval_max", "imax", "interval", "i"),
                Enforce_Min = ReadInt(scriptObj, scriptObj["defaults"] as JObject, 0, -1, "enforce_min", "enforceMin", "minimumCycleSeconds", "minCycle", "emin"),
                DefaultOffsetEnabled = ReadBool(scriptObj, false, "defaultOffsetEnabled", "enableDefaultOffset"),
                DefaultOffset = ReadString(scriptObj, null, "0", "defaultOffset")
            };

            var config = scriptObj["config"] as JArray ?? scriptObj["steps"] as JArray ?? new JArray();
            script.Groups = ParseGroups(config, scriptObj["defaults"] as JObject).ToList();
            if (script.Groups.Count == 0)
            {
                script.Groups.Add(new ActionGroup());
            }

            return script;
        }

        private static IEnumerable<ActionGroup> ParseGroups(JArray config, JObject? defaults)
        {
            foreach (var item in config)
            {
                if (item is not JObject obj)
                {
                    continue;
                }

                if (obj["steps"] is JArray nested)
                {
                    yield return new ActionGroup
                    {
                        Repeat = Math.Max(1, ReadInt(obj, null, 1, -1, "repeat", "rep")),
                        Steps = nested.OfType<JObject>().Select(step => ParseStep(step, defaults)).ToList()
                    };
                }
                else
                {
                    yield return new ActionGroup
                    {
                        Repeat = 1,
                        Steps = new List<StepAction> { ParseStep(obj, defaults) }
                    };
                }
            }
        }

        private static SequenceModel ParseSequence(string key, JObject sequenceObj, int fallbackOrder)
        {
            var sequence = new SequenceModel
            {
                Id = ReadString(sequenceObj, null, NewId("seq"), "id"),
                Name = ReadString(sequenceObj, null, key, "name"),
                Tag = ReadString(sequenceObj, null, "", "tag"),
                Order = ReadInt(sequenceObj, null, fallbackOrder, -1, "order"),
                Duration = ReadInt(sequenceObj, null, 1, -1, "duration", "d"),
                Interval_Min = ReadInt(sequenceObj, null, 0, 0, "interval_min", "imin", "interval", "i"),
                Interval_Max = ReadInt(sequenceObj, null, 0, 1, "interval_max", "imax", "interval", "i"),
                Enforce_Min = ReadInt(sequenceObj, null, 0, -1, "enforce_min", "enforceMin", "minimumCycleSeconds", "minCycle", "emin"),
                DefaultOffsetEnabled = ReadBool(sequenceObj, false, "defaultOffsetEnabled", "enableDefaultOffset"),
                DefaultOffset = ReadString(sequenceObj, null, "0", "defaultOffset")
            };

            if (sequenceObj["items"] is JArray items)
            {
                foreach (var item in items.OfType<JObject>())
                {
                    string type = ReadString(item, null, "script", "type").ToLowerInvariant();
                    var sequenceItem = new SequenceItem
                    {
                        Type = type == "action" ? "action" : "script",
                        ScriptId = ReadString(item, null, "", "scriptId"),
                        Repeat = Math.Max(1, ReadInt(item, null, 1, -1, "repeat")),
                        Interval_Min = ReadInt(item, null, 0, -1, "imin", "interval_min"),
                        Interval_Max = ReadInt(item, null, 0, -1, "imax", "interval_max"),
                        Action = ParseStep(item, null)
                    };
                    sequence.Items.Add(sequenceItem);
                }
            }

            return sequence;
        }

        public static JObject BuildScriptJson(ScriptModel script)
        {
            var config = new JArray();
            foreach (var group in script.Groups)
            {
                var steps = new JArray(group.Steps.Select(BuildStepJson));
                config.Add(new JObject
                {
                    ["steps"] = steps,
                    ["repeat"] = Math.Max(1, group.Repeat)
                });
            }

            return new JObject
            {
                ["id"] = string.IsNullOrWhiteSpace(script.Id) ? NewId("scr") : script.Id,
                ["name"] = script.Name,
                ["tag"] = script.Tag,
                ["hide"] = script.Hidden,
                ["order"] = script.Order,
                ["d"] = script.Duration,
                ["imin"] = script.Interval_Min,
                ["imax"] = script.Interval_Max,
                ["emin"] = Math.Max(0, script.Enforce_Min),
                ["defaultOffsetEnabled"] = script.DefaultOffsetEnabled,
                ["defaultOffset"] = string.IsNullOrWhiteSpace(script.DefaultOffset) ? "0" : script.DefaultOffset,
                ["config"] = config
            };
        }

        public static JObject BuildSequenceJson(SequenceModel sequence)
        {
            return new JObject
            {
                ["id"] = string.IsNullOrWhiteSpace(sequence.Id) ? NewId("seq") : sequence.Id,
                ["name"] = sequence.Name,
                ["tag"] = sequence.Tag,
                ["order"] = sequence.Order,
                ["d"] = sequence.Duration,
                ["imin"] = sequence.Interval_Min,
                ["imax"] = sequence.Interval_Max,
                ["emin"] = Math.Max(0, sequence.Enforce_Min),
                ["defaultOffsetEnabled"] = sequence.DefaultOffsetEnabled,
                ["defaultOffset"] = string.IsNullOrWhiteSpace(sequence.DefaultOffset) ? "0" : sequence.DefaultOffset,
                ["items"] = new JArray(sequence.Items.Select(BuildSequenceItemJson))
            };
        }

        public static JObject BuildSequenceItemJson(SequenceItem item)
        {
            if (item.Type == "action")
            {
                var action = BuildStepJson(item.Action);
                action["type"] = "action";
                return action;
            }

            return new JObject
            {
                ["type"] = "script",
                ["scriptId"] = item.ScriptId,
                ["repeat"] = Math.Max(1, item.Repeat),
                ["imin"] = item.Interval_Min,
                ["imax"] = item.Interval_Max
            };
        }

        public static JObject BuildStepJson(StepAction step)
        {
            var action = NormalizeAction(step.Act);
            var obj = new JObject
            {
                ["a"] = action,
                ["s"] = new JArray(step.ScrX, step.ScrY),
                ["r"] = new JArray(step.RandX, step.RandY),
                ["t"] = new JArray(step.Sleep_Min, step.Sleep_Max)
            };

            if (action == "drag" && (step.ScrX2.HasValue || step.ScrY2.HasValue))
            {
                obj["s2"] = new JArray(step.ScrX2 ?? 0, step.ScrY2 ?? 0);
            }

            if (!string.IsNullOrWhiteSpace(step.Offset))
            {
                obj["o"] = step.Offset;
            }

            return obj;
        }

        private static StepAction ParseStep(JObject stepObj, JObject? defaults)
        {
            return new StepAction
            {
                Act = NormalizeAction(ReadString(stepObj, defaults, "leftclick", "act", "a")),
                ScrX = ReadInt(stepObj, defaults, 0, 0, "scrX", "sx", "x", "posX", "px", "scr", "s", "p"),
                ScrY = ReadInt(stepObj, defaults, 0, 1, "scrY", "sy", "y", "posY", "py", "scr", "s", "p"),
                ScrX2 = ReadNullableInt(stepObj, defaults, 0, "scrX2", "sx2", "x2", "posX2", "px2", "scr2", "s2", "p2"),
                ScrY2 = ReadNullableInt(stepObj, defaults, 1, "scrY2", "sy2", "y2", "posY2", "py2", "scr2", "s2", "p2"),
                RandX = ReadInt(stepObj, defaults, 0, 0, "randX", "rx", "rand", "r"),
                RandY = ReadInt(stepObj, defaults, 0, 1, "randY", "ry", "rand", "r"),
                Sleep_Min = ReadInt(stepObj, defaults, 0, 0, "sleep_min", "smin", "sleep", "t"),
                Sleep_Max = ReadInt(stepObj, defaults, 0, 1, "sleep_max", "smax", "sleep", "t"),
                Offset = ReadString(stepObj, defaults, "", "offset", "o")
            };
        }

        private static string NormalizeAction(string action)
        {
            return (action ?? "").Trim().ToLowerInvariant() switch
            {
                "" => "left",
                "leftclick" => "left",
                "rightclick" => "right",
                "left" => "left",
                "right" => "right",
                "back" => "right",
                "drag" => "drag",
                "leftdrag" => "drag",
                "rightdrag" => "drag",
                "updrag" => "drag",
                "downdrag" => "drag",
                _ => "left"
            };
        }

        private static string ReadString(JObject source, JObject? defaults, string fallback, params string[] aliases)
        {
            var token = TryGetToken(source, defaults, aliases);
            return string.IsNullOrWhiteSpace(token?.ToString()) ? fallback : token!.ToString();
        }

        private static int ReadInt(JObject source, JObject? defaults, int fallback, int index, params string[] aliases)
        {
            var token = TryGetToken(source, defaults, aliases);
            return TryParseIntToken(token, index, out var value) ? value : fallback;
        }

        private static int? ReadNullableInt(JObject source, JObject? defaults, int index, params string[] aliases)
        {
            var token = TryGetToken(source, defaults, aliases);
            return TryParseIntToken(token, index, out var value) ? value : null;
        }

        private static int ReadIntOrFallback(JObject source, int fallback, int index, params string[] aliases)
        {
            var token = TryGetToken(source, null, aliases);
            return TryParseIntToken(token, index, out var value) ? value : fallback;
        }

        private static bool ReadBool(JObject source, bool fallback, params string[] aliases)
        {
            var token = TryGetToken(source, null, aliases);
            return bool.TryParse(token?.ToString(), out bool parsed) ? parsed : fallback;
        }

        private static JToken? TryGetToken(JObject source, JObject? defaults, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                var token = source.GetValue(alias, StringComparison.OrdinalIgnoreCase);
                if (token != null)
                {
                    return token;
                }

                token = defaults?.GetValue(alias, StringComparison.OrdinalIgnoreCase);
                if (token != null)
                {
                    return token;
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

        public static string NewId(string prefix)
        {
            return prefix + "_" + Guid.NewGuid().ToString("N")[..8];
        }
    }
}
