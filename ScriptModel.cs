namespace Lazy_App_Codex_Core
{
    public sealed class ConfigLibrary
    {
        public List<ScriptModel> Scripts { get; set; } = new();
        public List<SequenceModel> Sequences { get; set; } = new();

        public ScriptModel? FindScriptById(string? id)
        {
            return Scripts.FirstOrDefault(script => script.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class ScriptModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Tag { get; set; } = "";
        public bool Hidden { get; set; }
        public int Order { get; set; }
        public int Duration { get; set; }        // 0 = unlimited, >0 = loop count
        public int Interval_Min { get; set; }    // seconds
        public int Interval_Max { get; set; }    // seconds
        public int Enforce_Min { get; set; }     // seconds, 0 = disabled
        public bool DefaultOffsetEnabled { get; set; }
        public string DefaultOffset { get; set; } = "0";
        public List<ActionGroup> Groups { get; set; } = new();

        public List<StepAction> Config => Groups.SelectMany(group => group.ExpandedSteps()).ToList();
    }

    public sealed class ActionGroup
    {
        public int Repeat { get; set; } = 1;
        public List<StepAction> Steps { get; set; } = new();

        public IEnumerable<StepAction> ExpandedSteps()
        {
            int repeat = Math.Max(1, Repeat);
            for (int i = 0; i < repeat; i++)
            {
                foreach (var step in Steps)
                {
                    yield return step;
                }
            }
        }
    }

    public sealed class SequenceModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Tag { get; set; } = "";
        public int Order { get; set; }
        public int Duration { get; set; }
        public int Interval_Min { get; set; }
        public int Interval_Max { get; set; }
        public int Enforce_Min { get; set; }
        public bool DefaultOffsetEnabled { get; set; }
        public string DefaultOffset { get; set; } = "0";
        public List<SequenceItem> Items { get; set; } = new();
    }

    public sealed class SequenceItem
    {
        public string Type { get; set; } = "script";
        public string ScriptId { get; set; } = "";
        public int Repeat { get; set; } = 1;
        public int Interval_Min { get; set; }
        public int Interval_Max { get; set; }
        public StepAction Action { get; set; } = new();
    }

    public sealed class StepAction
    {
        public string Act { get; set; } = "";   // e.g. leftclick, drag, rightclick
        public int ScrX { get; set; }
        public int ScrY { get; set; }
        public int? ScrX2 { get; set; }
        public int? ScrY2 { get; set; }
        public int RandX { get; set; }
        public int RandY { get; set; }
        public int Sleep_Min { get; set; }
        public int Sleep_Max { get; set; }
        public string Offset { get; set; } = "";
    }
}
