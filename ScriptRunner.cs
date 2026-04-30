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
            bool isAdbEnabled)
        {
            var adb = new AdbShellController();

            if (script.Duration <= 0)
            {
                long loop = 1;
                while (true)
                {
                    await RunLoopAsync(script, loop, "unlimited", selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
                    loop++;
                }
            }

            for (long loop = 1; loop <= script.Duration; loop++)
            {
                await RunLoopAsync(script, loop, script.Duration.ToString(), selectedOffset, selectedOffsetAxis, adb, token, onStatus, isAdbEnabled);
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
            bool isAdbEnabled)
        {
            token.ThrowIfCancellationRequested();
            onStatus($"CLICKING {loop}/{loopTotal}", Color.Red);

            for (int stepIndex = 0; stepIndex < script.Config.Count; stepIndex++)
            {
                var step = script.Config[stepIndex];
                token.ThrowIfCancellationRequested();
                int randSleep = step.Sleep_Max > 0 ? RandomBetween(step.Sleep_Min, step.Sleep_Max) : 0;

                int stepOffset = stepIndex == 0 ? selectedOffset : 0;
                await ExecuteStepAsync(step, stepOffset, selectedOffsetAxis, adb, randSleep, token, onStatus, isAdbEnabled);

                if (randSleep > 0)
                {
                    await Task.Delay(randSleep * 1000, token);
                }
            }

            int intervalSleep = script.Interval_Max > 0 ? RandomBetween(script.Interval_Min, script.Interval_Max) : 0;
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

        private async Task ExecuteStepAsync(
            StepAction step,
            int selectedOffset,
            string selectedOffsetAxis,
            AdbShellController adb,
            int randSleep,
            CancellationToken token,
            Action<string, Color> onStatus,
            bool isAdbEnabled)
        {
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

                onStatus($"LEFT CLICK ({x},{y}) OFFSET {FormatOffset(selectedOffset, axis)}:{randSleep}(s)", Color.Red);
                if (!isAdbEnabled)
                {
                    onStatus("ADB OFF: SKIP TAP", Color.DarkOrange);
                    return;
                }

                await RunAdbCommandAsync(adb, $"shell input tap {x} {y}", "tap", token);
                return;
            }

            if (step.Act == "rightclick" || step.Act == "right")
            {
                onStatus("BACK BUTTON CLICK", Color.Red);
                if (!isAdbEnabled)
                {
                    onStatus("ADB OFF: SKIP KEY BACK", Color.DarkOrange);
                    return;
                }

                await RunAdbCommandAsync(adb, $"shell input keyevent {AndroidKeys.BACK}", "key back", token);
                return;
            }

            if (step.Act == "drag" || step.Act == "leftdrag" || step.Act == "rightdrag" || step.Act == "updrag" || step.Act == "downdrag")
            {
                var p1 = MouseHelper.WithRandomDrag(step.ScrX, step.ScrY, step.RandX, step.RandY, false);
                var p2 = MouseHelper.WithRandomDrag(step.ScrX2 ?? step.ScrX, step.ScrY2 ?? step.ScrY, step.RandX, step.RandY, false);
                onStatus($"DRAG ({p1.Item1},{p1.Item2}):({p2.Item1},{p2.Item2}):{randSleep}(s)", Color.Red);
                if (!isAdbEnabled)
                {
                    onStatus("ADB OFF: SKIP SWIPE", Color.DarkOrange);
                    return;
                }

                await RunAdbCommandAsync(adb, $"shell input swipe {p1.Item1} {p1.Item2} {p2.Item1} {p2.Item2} 150", "swipe", token);
            }
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
    }
}
