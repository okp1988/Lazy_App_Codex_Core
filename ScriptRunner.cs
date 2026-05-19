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
            string deviceSerial,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            var adb = new AdbShellController(deviceSerial: deviceSerial);
            if (script.Duration <= 0)
            {
                long loop = 1;
                while (true)
                {
                    await RunLoopAsync(script, loop, 0, selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
                    loop++;
                }
            }

            await RunScriptForCyclesAsync(script, script.Duration, selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
        }

        public async Task RunSequenceAsync(
            SequenceModel sequence,
            ConfigLibrary library,
            int selectedOffset,
            string selectedOffsetAxis,
            Func<ScriptModel, (int value, string axis)> scriptOffsetResolver,
            string deviceSerial,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            var adb = new AdbShellController(deviceSerial: deviceSerial);
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

        public async Task RunPlanAsync(
            RunPlanModel runPlan,
            ConfigLibrary library,
            Func<ScriptModel, (int value, string axis)> scriptOffsetResolver,
            Func<SequenceModel, (int value, string axis)> sequenceOffsetResolver,
            Func<SequenceModel, ScriptModel, (int value, string axis)> sequenceScriptOffsetResolver,
            string deviceSerial,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            if (runPlan.Items.Count == 0)
            {
                throw new InvalidOperationException($"Run plan \"{runPlan.Name}\" has no items.");
            }

            var adb = new AdbShellController(deviceSerial: deviceSerial);
            foreach (var item in runPlan.Items)
            {
                token.ThrowIfCancellationRequested();
                int cycles = Math.Max(1, item.Repeat);
                if (item.Type == "sequence")
                {
                    var sequence = library.FindSequenceById(item.TargetId)
                        ?? throw new InvalidOperationException($"Run plan \"{runPlan.Name}\" references missing sequence \"{item.TargetId}\".");
                    var offset = sequenceOffsetResolver(sequence);
                    await RunSequenceForCyclesAsync(
                        sequence,
                        library,
                        cycles,
                        offset.value,
                        offset.axis,
                        script => sequenceScriptOffsetResolver(sequence, script),
                        adb,
                        token,
                        onStatus,
                        isAdbEnabled);
                    continue;
                }

                var script = library.FindScriptById(item.TargetId)
                    ?? throw new InvalidOperationException($"Run plan \"{runPlan.Name}\" references missing script \"{item.TargetId}\".");
                var scriptOffset = scriptOffsetResolver(script);
                await RunScriptForCyclesAsync(script, cycles, scriptOffset.value, scriptOffset.axis, adb, token, onStatus, isAdbEnabled);
            }
        }

        public async Task RunScriptForCyclesAsync(
            ScriptModel script,
            int cycleCount,
            int selectedOffset,
            string selectedOffsetAxis,
            AdbShellController adb,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            int cycles = Math.Max(1, cycleCount);
            for (long loop = 1; loop <= cycles; loop++)
            {
                await RunLoopAsync(script, loop, cycles, selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
            }
        }

        public async Task RunSequenceForCyclesAsync(
            SequenceModel sequence,
            ConfigLibrary library,
            int cycleCount,
            int selectedOffset,
            string selectedOffsetAxis,
            Func<ScriptModel, (int value, string axis)> scriptOffsetResolver,
            AdbShellController adb,
            CancellationToken token,
            Action<LiveRunStatus> onStatus,
            bool isAdbEnabled)
        {
            int cycles = Math.Max(1, cycleCount);
            for (long loop = 1; loop <= cycles; loop++)
            {
                await RunSequenceLoopAsync(sequence, library, loop, cycles, selectedOffset, selectedOffsetAxis, scriptOffsetResolver, adb, token, onStatus, isAdbEnabled);
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
            intervalSleep = EnforceCycleMinimum(plannedSteps, intervalSleep, sequence.Interval_Min, sequence.Interval_Max, sequence.Enforce_Min);
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
            intervalSleep = EnforceCycleMinimum(plannedSteps, intervalSleep, script.Interval_Min, script.Interval_Max, script.Enforce_Min);
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
            var sleepRange = NormalizeRange(step.Sleep_Min, step.Sleep_Max);
            int sleepMin = sleepRange.min;
            int sleepMax = sleepRange.max;
            int randSleep = sleepMax > 0 ? RandomBetween(sleepMin, sleepMax) : 0;
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

                return new PlannedStep("LEFT", $"shell input tap {x} {y}", "tap", randSleep, sleepMin, sleepMax);
            }

            if (action == "right")
            {
                return new PlannedStep("BACK", $"shell input keyevent {AndroidKeys.BACK}", "key back", randSleep, sleepMin, sleepMax);
            }

            if (action == "drag")
            {
                var p1 = MouseHelper.WithRandom(step.ScrX, step.ScrY, step.RandX, step.RandY);
                var end = GetDragEndPoint(step);
                var p2 = MouseHelper.WithRandom(end.x, end.y, step.RandX, step.RandY);
                return new PlannedStep("DRAG", $"shell input swipe {p1.Item1} {p1.Item2} {p2.Item1} {p2.Item2} 150", "swipe", randSleep, sleepMin, sleepMax);
            }

            return new PlannedStep("--", "", "", randSleep, sleepMin, sleepMax);
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

        private static (int min, int max) NormalizeRange(int min, int max)
        {
            min = Math.Max(0, min);
            max = Math.Max(0, max);
            return max < min ? (max, min) : (min, max);
        }

        private void AddDelayToLastStep(List<PlannedStep> plannedSteps, int min, int max)
        {
            if (plannedSteps.Count == 0 || max <= 0)
            {
                return;
            }

            var delayRange = NormalizeRange(min, max);
            plannedSteps[^1].AddSleep(RandomBetween(delayRange.min, delayRange.max), delayRange.min, delayRange.max);
        }

        private int EnforceCycleMinimum(
            List<PlannedStep> plannedSteps,
            int intervalSleep,
            int intervalMin,
            int intervalMax,
            int enforceMin)
        {
            var intervalRange = NormalizeRange(intervalMin, intervalMax);
            int maxCycleSeconds = plannedSteps.Sum(step => step.SleepMax) + intervalRange.max;
            int targetSeconds = Math.Clamp(enforceMin, 0, maxCycleSeconds);
            if (targetSeconds <= 0)
            {
                return intervalSleep;
            }

            if (targetSeconds == maxCycleSeconds)
            {
                foreach (var step in plannedSteps)
                {
                    step.UseMaxSleep();
                }

                return intervalRange.max;
            }

            int intervalMinimum = intervalRange.min;
            int intervalMaximum = intervalRange.max;
            while (plannedSteps.Sum(step => step.SleepSeconds) + intervalSleep < targetSeconds)
            {
                var nextStep = plannedSteps
                    .Where(step => step.SleepSeconds < step.SleepMax)
                    .OrderBy(step => step.SleepSeconds)
                    .ThenBy(step => step.SleepMax)
                    .FirstOrDefault();

                bool canIncreaseInterval = intervalSleep < intervalMaximum;
                if (nextStep == null && !canIncreaseInterval)
                {
                    break;
                }

                if (nextStep != null && (!canIncreaseInterval || nextStep.SleepSeconds <= intervalSleep))
                {
                    nextStep.RerollSleepUp(RandomBetween);
                    continue;
                }

                intervalSleep = RandomBetween(Math.Max(intervalMinimum, intervalSleep + 1), intervalMaximum);
            }

            return intervalSleep;
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
            public PlannedStep(string shortName, string adbArgs, string adbAction, int sleepSeconds, int sleepMin, int sleepMax)
            {
                ShortName = shortName;
                AdbArgs = adbArgs;
                AdbAction = adbAction;
                SleepSeconds = sleepSeconds;
                SleepMin = sleepMin;
                SleepMax = sleepMax;
            }

            public string ShortName { get; }
            public string AdbArgs { get; }
            public string AdbAction { get; }
            public int SleepSeconds { get; private set; }
            public int SleepMin { get; private set; }
            public int SleepMax { get; private set; }
            public DateTime? EstimatedEnd { get; set; }

            public void AddSleep(int seconds, int min, int max)
            {
                SleepSeconds += Math.Max(0, seconds);
                SleepMin += Math.Max(0, min);
                SleepMax += Math.Max(0, max);
            }

            public void UseMaxSleep()
            {
                SleepSeconds = SleepMax;
            }

            public void RerollSleepUp(Func<int, int, int> randomBetween)
            {
                if (SleepSeconds >= SleepMax)
                {
                    return;
                }

                SleepSeconds = randomBetween(Math.Max(SleepMin, SleepSeconds + 1), SleepMax);
            }
        }
    }
}
