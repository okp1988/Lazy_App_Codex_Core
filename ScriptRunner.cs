using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lazy_App_Codex_Core
{
    public class ScriptRunner
    {
        private readonly Random _random = new Random();

        public async Task RunAsync(
            ScriptModel script,
            int selectedOffset,
            string selectedOffsetAxis,
            CancellationToken token,
            Action<string, Color> onStatus,
            Action<string> onCircleTiming,
            bool isAdbEnabled)
        {
            var adb = new AdbShellController();

            if (script.Duration <= 0)
            {
                long loop = 1;
                while (true)
                {
                    await RunLoopAsync(script, loop, "unlimited", selectedOffset, selectedOffsetAxis, adb, token, onStatus, onCircleTiming, isAdbEnabled);
                    loop++;
                }
            }

            for (long loop = 1; loop <= script.Duration; loop++)
            {
                await RunLoopAsync(script, loop, script.Duration.ToString(), selectedOffset, selectedOffsetAxis, adb, token, onStatus, onCircleTiming, isAdbEnabled);
            }
        }

        private async Task RunLoopAsync(
            ScriptModel script,
            long loop,
            string loopTotal,
            int selectedOffset,
            string selectedOffsetAxis,
            AdbShellController adb,
            CancellationToken token,
            Action<string, Color> onStatus,
            Action<string> onCircleTiming,
            bool isAdbEnabled)
        {
            token.ThrowIfCancellationRequested();
            onStatus($"GENERATING {loop}/{loopTotal}", Color.DarkOrange);

            List<PlannedStep> plannedSteps = new List<PlannedStep>();
            for (int stepIndex = 0; stepIndex < script.Config.Count; stepIndex++)
            {
                int stepOffset = stepIndex == 0 ? selectedOffset : 0;
                plannedSteps.Add(GenerateStep(script.Config[stepIndex], stepOffset, selectedOffsetAxis));
            }

            int intervalSleep = script.Interval_Max > 0 ? RandomBetween(script.Interval_Min, script.Interval_Max) : 0;
            int circleSeconds = plannedSteps.Sum(step => step.SleepSeconds) + intervalSleep;
            DateTime expectedEnd = DateTime.Now.AddSeconds(circleSeconds);
            onCircleTiming($"Circle: {loop} | Time: {FormatDuration(circleSeconds)} | End: {expectedEnd:HH:mm:ss}");

            token.ThrowIfCancellationRequested();
            onStatus($"CLICKING {loop}/{loopTotal}", Color.Red);

            foreach (PlannedStep plannedStep in plannedSteps)
            {
                token.ThrowIfCancellationRequested();
                await ExecutePlannedStepAsync(plannedStep, adb, token, onStatus, isAdbEnabled);

                if (plannedStep.SleepSeconds > 0)
                {
                    await Task.Delay(plannedStep.SleepSeconds * 1000, token);
                }
            }

            onStatus($"DONE {loop}/{loopTotal}:{intervalSleep}(s)", Color.Red);

            if (intervalSleep > 0)
            {
                await Task.Delay(intervalSleep * 1000, token);
            }
        }

        private static async Task RunAdbCommandAsync(
            AdbShellController adb,
            string args,
            string action,
            CancellationToken token)
        {
            var (exitCode, stdout, stderr) = await adb.RunCaptureAsync(args, token);
            token.ThrowIfCancellationRequested();
            EnsureAdbSucceeded(exitCode, action, stdout, stderr);
        }

        private static void EnsureAdbSucceeded(int exitCode, string action, string stdout, string stderr)
        {
            if (exitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                detail = string.IsNullOrWhiteSpace(detail) ? "No ADB error output." : detail.Trim();
                throw new InvalidOperationException($"ADB {action} failed with exit code {exitCode}: {detail}");
            }
        }

        private PlannedStep GenerateStep(
            StepAction step,
            int selectedOffset,
            string selectedOffsetAxis)
        {
            int randSleep = step.Sleep_Max > 0 ? RandomBetween(step.Sleep_Min, step.Sleep_Max) : 0;

            if (step.Act == "leftclick" || step.Act == "left")
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

                return new PlannedStep(
                    $"LEFT CLICK ({x},{y}) OFFSET {FormatOffset(selectedOffset, axis)}:{randSleep}(s)",
                    $"shell input tap {x} {y}",
                    "tap",
                    "ADB OFF: SKIP TAP",
                    randSleep);
            }

            if (step.Act == "rightclick" || step.Act == "right")
            {
                return new PlannedStep(
                    "BACK BUTTON CLICK",
                    $"shell input keyevent {AndroidKeys.BACK}",
                    "key back",
                    "ADB OFF: SKIP KEY BACK",
                    randSleep);
            }

            if (step.Act == "drag" || step.Act == "leftdrag" || step.Act == "rightdrag" || step.Act == "updrag" || step.Act == "downdrag")
            {
                var p1 = MouseHelper.WithRandomDrag(step.ScrX, step.ScrY, step.RandX, step.RandY, false);
                var p2 = MouseHelper.WithRandomDrag(step.ScrX2 ?? step.ScrX, step.ScrY2 ?? step.ScrY, step.RandX, step.RandY, false);
                return new PlannedStep(
                    $"DRAG ({p1.Item1},{p1.Item2}):({p2.Item1},{p2.Item2}):{randSleep}(s)",
                    $"shell input swipe {p1.Item1} {p1.Item2} {p2.Item1} {p2.Item2} 150",
                    "swipe",
                    "ADB OFF: SKIP SWIPE",
                    randSleep);
            }

            return new PlannedStep($"UNKNOWN ACTION {step.Act}:{randSleep}(s)", "", "", "ADB OFF: SKIP UNKNOWN", randSleep);
        }

        private static async Task ExecutePlannedStepAsync(
            PlannedStep plannedStep,
            AdbShellController adb,
            CancellationToken token,
            Action<string, Color> onStatus,
            bool isAdbEnabled)
        {
            onStatus(plannedStep.Status, Color.Red);
            if (string.IsNullOrWhiteSpace(plannedStep.AdbArgs))
            {
                return;
            }

            if (!isAdbEnabled)
            {
                onStatus(plannedStep.AdbDisabledStatus, Color.DarkOrange);
                return;
            }

            await RunAdbCommandAsync(adb, plannedStep.AdbArgs, plannedStep.AdbAction, token);
        }

        private int RandomBetween(int min, int max)
        {
            if (max < min)
            {
                int temp = min;
                min = max;
                max = temp;
            }

            lock (_random)
            {
                return _random.Next(min, max + 1);
            }
        }

        private static string FormatOffset(int value, string axis)
        {
            string sign = value > 0 ? "+" : "";
            return $"{sign}{value}{axis}";
        }

        private static string FormatDuration(int seconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }

            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        private sealed class PlannedStep
        {
            public PlannedStep(string status, string adbArgs, string adbAction, string adbDisabledStatus, int sleepSeconds)
            {
                Status = status;
                AdbArgs = adbArgs;
                AdbAction = adbAction;
                AdbDisabledStatus = adbDisabledStatus;
                SleepSeconds = sleepSeconds;
            }

            public string Status { get; }
            public string AdbArgs { get; }
            public string AdbAction { get; }
            public string AdbDisabledStatus { get; }
            public int SleepSeconds { get; }
        }
    }
}
