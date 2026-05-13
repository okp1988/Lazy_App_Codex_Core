namespace Lazy_App_Codex_Core
{
    public sealed class LiveRunStatus
    {
        public string CurrentAction { get; set; } = "--";
        public string CurrentStep { get; set; } = "--";
        public string CurrentCycle { get; set; } = "--";
        public string NextAction { get; set; } = "--";
        public DateTime? NextActionAt { get; set; }
        public DateTime? EstimatedEnd { get; set; }
        public bool Idle { get; set; }
    }

    public class ScriptRunner
    {
        private readonly Random _random = new();

        public async Task RunScriptAsync(
            ScriptModel script,
            int selectedOffset,
            string selectedOffsetAxis,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            var adb = new AdbShellController();
            if (script.Duration <= 0)
            {
                long loop = 1;
                while (true)
                {
                    await RunLoopAsync(script, loop, 0, selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
                    loop++;
                }
            }

            for (long loop = 1; loop <= script.Duration; loop++)
            {
                await RunLoopAsync(script, loop, script.Duration, selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
            }
        }

        public async Task RunSequenceAsync(
            SequenceModel sequence,
            ConfigLibrary library,
            int selectedOffset,
            string selectedOffsetAxis,
            Func<ScriptModel, (int value, string axis)> scriptOffsetResolver,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            var adb = new AdbShellController();
            if (sequence.Duration <= 0)
            {
                long loop = 1;
                while (true)
                {
                    await RunSequenceLoopAsync(sequence, library, loop, 0, selectedOffset, selectedOffsetAxis, scriptOffsetResolver, adb, token, onStatus, isAdbEnabled);
                    loop++;
                }
            }

            for (long loop = 1; loop <= sequence.Duration; loop++)
            {
                await RunSequenceLoopAsync(sequence, library, loop, sequence.Duration, selectedOffset, selectedOffsetAxis, scriptOffsetResolver, adb, token, onStatus, isAdbEnabled);
            }
        }

        private async Task RunSequenceLoopAsync(
            SequenceModel sequence,
            ConfigLibrary library,
            long loop,
            int loopTotal,
            int selectedOffset,
            string selectedOffsetAxis,
            Func<ScriptModel, (int value, string axis)> scriptOffsetResolver,
            AdbShellController adb,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            var plannedSteps = BuildSequencePlan(sequence, library, selectedOffset, selectedOffsetAxis, scriptOffsetResolver);
            int intervalSleep = sequence.Interval_Max > 0 ? RandomBetween(sequence.Interval_Min, sequence.Interval_Max) : 0;
            DateTime estimatedEnd = DateTime.Now.AddSeconds(plannedSteps.Sum(step => step.SleepSeconds) + intervalSleep);
            for (int index = 0; index < plannedSteps.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                plannedSteps[index].EstimatedEnd = estimatedEnd;
                string nextAction = index + 1 < plannedSteps.Count ? plannedSteps[index + 1].ShortName : "--";
                await ExecutePlannedStepAsync(plannedSteps[index], index + 1, plannedSteps.Count, loop, loopTotal, adb, token, onStatus, isAdbEnabled, nextAction);
            }

            if (intervalSleep > 0)
            {
                onStatus(new LiveRunStatus
                {
                    CurrentAction = "--",
                    CurrentStep = "--",
                    CurrentCycle = loopTotal <= 0 ? $"{loop} / ∞" : $"{loop} / {loopTotal}",
                    NextAction = plannedSteps.FirstOrDefault()?.ShortName ?? "--",
                    NextActionAt = DateTime.Now.AddSeconds(intervalSleep),
                    EstimatedEnd = estimatedEnd
                });
                await Task.Delay(intervalSleep * 1000, token);
            }
        }

        private List<PlannedStep> BuildSequencePlan(
            SequenceModel sequence,
            ConfigLibrary library,
            int selectedOffset,
            string selectedOffsetAxis,
            Func<ScriptModel, (int value, string axis)> scriptOffsetResolver)
        {
            var plannedSteps = new List<PlannedStep>();
            for (int itemIndex = 0; itemIndex < sequence.Items.Count; itemIndex++)
            {
                var item = sequence.Items[itemIndex];
                if (item.Type == "script")
                {
                    var script = library.FindScriptById(item.ScriptId);
                    if (script == null)
                    {
                        continue;
                    }

                    for (int repeat = 1; repeat <= Math.Max(1, item.Repeat); repeat++)
                    {
                        var scriptOffset = scriptOffsetResolver(script);
                        for (int stepIndex = 0; stepIndex < script.Config.Count; stepIndex++)
                        {
                            int stepOffset = stepIndex == 0 ? scriptOffset.value : 0;
                            plannedSteps.Add(GenerateStep(script.Config[stepIndex], stepOffset, scriptOffset.axis));
                        }
                    }

                    AddDelayToLastStep(plannedSteps, item.Interval_Min, item.Interval_Max);
                }
                else
                {
                    int actionOffset = plannedSteps.Count == 0 ? selectedOffset : 0;
                    plannedSteps.Add(GenerateStep(item.Action, actionOffset, selectedOffsetAxis));
                }
            }

            return plannedSteps;
        }

        private async Task RunLoopAsync(
            ScriptModel script,
            long loop,
            int loopTotal,
            int selectedOffset,
            string selectedOffsetAxis,
            AdbShellController adb,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            token.ThrowIfCancellationRequested();

            var plannedSteps = new List<PlannedStep>();
            for (int stepIndex = 0; stepIndex < script.Config.Count; stepIndex++)
            {
                int stepOffset = stepIndex == 0 ? selectedOffset : 0;
                plannedSteps.Add(GenerateStep(script.Config[stepIndex], stepOffset, selectedOffsetAxis));
            }

            int intervalSleep = script.Interval_Max > 0 ? RandomBetween(script.Interval_Min, script.Interval_Max) : 0;
            int totalSeconds = plannedSteps.Sum(step => step.SleepSeconds) + intervalSleep;
            DateTime estimatedEnd = DateTime.Now.AddSeconds(totalSeconds);

            for (int index = 0; index < plannedSteps.Count; index++)
            {
                plannedSteps[index].EstimatedEnd = estimatedEnd;
                string nextAction = index + 1 < plannedSteps.Count ? plannedSteps[index + 1].ShortName : "--";
                await ExecutePlannedStepAsync(plannedSteps[index], index + 1, plannedSteps.Count, loop, loopTotal, adb, token, onStatus, isAdbEnabled, nextAction);
            }

            if (intervalSleep > 0)
            {
                onStatus(new LiveRunStatus
                {
                    CurrentAction = "--",
                    CurrentStep = $"--",
                    CurrentCycle = loopTotal <= 0 ? $"{loop} / ∞" : $"{loop} / {loopTotal}",
                    NextAction = plannedSteps.FirstOrDefault()?.ShortName ?? "--",
                    NextActionAt = DateTime.Now.AddSeconds(intervalSleep),
                    EstimatedEnd = estimatedEnd
                });
                await Task.Delay(intervalSleep * 1000, token);
            }
        }

        private static async Task RunAdbCommandAsync(AdbShellController adb, string args, string action, CancellationToken token)
        {
            var (exitCode, stdout, stderr) = await adb.RunCaptureAsync(args, token);
            token.ThrowIfCancellationRequested();
            if (exitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                detail = string.IsNullOrWhiteSpace(detail) ? "No ADB error output." : detail.Trim();
                throw new InvalidOperationException($"ADB {action} failed with exit code {exitCode}: {detail}");
            }
        }

        private PlannedStep GenerateStep(StepAction step, int selectedOffset, string selectedOffsetAxis)
        {
            int randSleep = step.Sleep_Max > 0 ? RandomBetween(step.Sleep_Min, step.Sleep_Max) : 0;
            string action = NormalizeAction(step.Act);

            if (action == "left")
            {
                var p = MouseHelper.WithRandom(step.ScrX, step.ScrY, step.RandX, step.RandY);
                int x = p.Item1;
                int y = p.Item2;
                string axis = string.IsNullOrWhiteSpace(step.Offset) ? selectedOffsetAxis : step.Offset;
                if (string.Equals(axis, "x", StringComparison.OrdinalIgnoreCase))
                {
                    x += selectedOffset;
                }
                else
                {
                    y += selectedOffset;
                }

                return new PlannedStep("LEFT", $"shell input tap {x} {y}", "tap", randSleep);
            }

            if (action == "right")
            {
                return new PlannedStep("BACK", $"shell input keyevent {AndroidKeys.BACK}", "key back", randSleep);
            }

            if (action == "drag")
            {
                var p1 = MouseHelper.WithRandom(step.ScrX, step.ScrY, step.RandX, step.RandY);
                var end = GetDragEndPoint(step);
                var p2 = MouseHelper.WithRandom(end.x, end.y, step.RandX, step.RandY);
                return new PlannedStep("DRAG", $"shell input swipe {p1.Item1} {p1.Item2} {p2.Item1} {p2.Item2} 150", "swipe", randSleep);
            }

            return new PlannedStep("--", "", "", randSleep);
        }

        private static (int x, int y) GetDragEndPoint(StepAction step)
        {
            if (step.ScrX2.HasValue || step.ScrY2.HasValue)
            {
                return (step.ScrX2 ?? step.ScrX, step.ScrY2 ?? step.ScrY);
            }

            return (step.ScrX, step.ScrY);
        }

        private static async Task ExecutePlannedStepAsync(
            PlannedStep plannedStep,
            int stepNumber,
            int stepTotal,
            long cycle,
            int cycleTotal,
            AdbShellController adb,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled,
            string nextAction = "--")
        {
            token.ThrowIfCancellationRequested();
            onStatus(new LiveRunStatus
            {
                CurrentAction = plannedStep.ShortName,
                CurrentStep = $"{stepNumber} / {stepTotal} ({Math.Max(0, stepTotal - stepNumber)})",
                CurrentCycle = cycleTotal <= 0 ? $"{cycle} / ∞" : $"{cycle} / {cycleTotal}",
                NextAction = nextAction,
                NextActionAt = plannedStep.SleepSeconds > 0 ? DateTime.Now.AddSeconds(plannedStep.SleepSeconds) : null,
                EstimatedEnd = plannedStep.EstimatedEnd
            });

            if (!string.IsNullOrWhiteSpace(plannedStep.AdbArgs) && isAdbEnabled)
            {
                await RunAdbCommandAsync(adb, plannedStep.AdbArgs, plannedStep.AdbAction, token);
            }

            if (plannedStep.SleepSeconds > 0)
            {
                await Task.Delay(plannedStep.SleepSeconds * 1000, token);
            }
        }

        private int RandomBetween(int min, int max)
        {
            if (max < min)
            {
                (min, max) = (max, min);
            }

            lock (_random)
            {
                return _random.Next(min, max + 1);
            }
        }

        private void AddDelayToLastStep(List<PlannedStep> plannedSteps, int min, int max)
        {
            if (plannedSteps.Count == 0 || max <= 0)
            {
                return;
            }

            plannedSteps[^1].AddSleep(RandomBetween(min, max));
        }

        private static string ShortActionName(SequenceItem item)
        {
            return item.Type == "script" ? "SCRIPT" : ShortActionName(item.Action);
        }

        private static string ShortActionName(StepAction action)
        {
            return NormalizeAction(action.Act) switch
            {
                "left" => "LEFT",
                "right" => "BACK",
                "drag" => "DRAG",
                _ => "--"
            };
        }

        private static string NormalizeAction(string action)
        {
            return action.Trim().ToLowerInvariant() switch
            {
                "leftclick" => "left",
                "rightclick" => "right",
                "back" => "right",
                "drag" => "drag",
                "left" => "left",
                "right" => "right",
                _ => "left"
            };
        }

        private sealed class PlannedStep
        {
            public PlannedStep(string shortName, string adbArgs, string adbAction, int sleepSeconds)
            {
                ShortName = shortName;
                AdbArgs = adbArgs;
                AdbAction = adbAction;
                SleepSeconds = sleepSeconds;
            }

            public string ShortName { get; }
            public string AdbArgs { get; }
            public string AdbAction { get; }
            public int SleepSeconds { get; private set; }
            public DateTime? EstimatedEnd { get; set; }

            public void AddSleep(int seconds)
            {
                SleepSeconds += Math.Max(0, seconds);
            }
        }
    }
}
