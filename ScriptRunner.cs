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
            int clickType,
            int selectedOffset,
            string selectedOffsetAxis,
            CancellationToken token,
            Action<string, Color> onStatus,
            bool isAdbEnabled)
        {
            var adb = new AdbShellController();
            int randSleep;

            int maxLoop = script.Duration == 0 ? short.MaxValue : script.Duration;
            for (int loop = 1; loop <= maxLoop; loop++)
            {
                token.ThrowIfCancellationRequested();
                onStatus($"CLICKING {loop}/{maxLoop}", Color.Red);

                foreach (var step in script.Config)
                {
                    token.ThrowIfCancellationRequested();
                    randSleep = step.Sleep_Max > 0 ? RandomBetween(step.Sleep_Min, step.Sleep_Max) : 0;

                    await ExecuteStepAsync(step, clickType, selectedOffset, selectedOffsetAxis, adb, randSleep, token, onStatus, isAdbEnabled);

                    if (randSleep > 0)
                    {
                        await Task.Delay(randSleep * 1000, token);
                    }
                }

                randSleep = script.Interval_Max > 0 ? RandomBetween(script.Interval_Min, script.Interval_Max) : 0;
                onStatus($"DONE {loop}/{maxLoop}:{randSleep}(s)", Color.Red);

                if (randSleep > 0)
                {
                    await Task.Delay(randSleep * 1000, token);
                }
            }
        }

        private async Task ExecuteStepAsync(
            StepAction step,
            int clickType,
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

                onStatus($"LEFT CLICK ({x},{y}):{randSleep}(s)", Color.Red);
                if (!isAdbEnabled)
                {
                    onStatus("ADB OFF: SKIP TAP", Color.DarkOrange);
                    return;
                }

                await adb.TapAsync(x, y, token);
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

                await adb.KeyAsync(AndroidKeys.BACK, token);
                return;
            }

            if (step.Act == "drag" || step.Act == "leftdrag" || step.Act == "rightdrag" || step.Act == "updrag" || step.Act == "downdrag")
            {
                var p1 = MouseHelper.WithRandomDrag(step.ScrX, step.ScrY, step.RandX, step.RandY, false);
                var p2 = MouseHelper.WithRandomDrag(step.ScrX2 ?? step.ScrX, step.ScrY2 ?? step.ScrY, step.RandX, step.RandY, false);
                int y1 = p1.Item2 + selectedOffset;
                int y2 = p2.Item2 + selectedOffset;
                onStatus($"DRAG ({p1.Item1},{y1}):({p2.Item1},{y2}):{randSleep}(s)", Color.Red);
                if (!isAdbEnabled)
                {
                    onStatus("ADB OFF: SKIP SWIPE", Color.DarkOrange);
                    return;
                }

                await adb.SwipeAsync(p1.Item1, y1, p2.Item1, y2, 150, token);
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
    }
}
