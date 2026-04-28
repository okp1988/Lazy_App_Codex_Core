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
            int yOffset,
            CancellationToken token,
            Action<string, Color> onStatus)
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

                    await ExecuteStepAsync(step, clickType, yOffset, adb, randSleep, token, onStatus);

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
            int yOffset,
            AdbShellController adb,
            int randSleep,
            CancellationToken token,
            Action<string, Color> onStatus)
        {
            if (step.Act == "leftclick")
            {
                //if (clickType == 2)
                //{
                //    var p = MouseHelper.WithRandom(step.PosX, step.PosY, step.RandX, step.RandY);
                //    int y = p.Item2 + yOffset;
                //    onStatus($"LEFT CLICK ({p.Item1},{y}):{randSleep}(s)", Color.Red);
                //    await MouseHelper.LeftClick(p.Item1, y);
                //}
                //else if (clickType == 1)
                //{
                //    var p = MouseHelper.WithRandom(step.ScrX, step.ScrY, step.RandX, step.RandY);
                //    int y = p.Item2 + yOffset;
                //    onStatus($"LEFT CLICK ({p.Item1},{y}):{randSleep}(s)", Color.Red);
                //    await WinClick.LeftClickScrcpyAtScreenAsync(p.Item1, y);
                //}
                //else
                //{
                var p = MouseHelper.WithRandom(step.ScrX, step.ScrY, step.RandX, step.RandY);
                int y = p.Item2 + yOffset;
                onStatus($"LEFT CLICK ({p.Item1},{y}):{randSleep}(s)", Color.Red);
                await adb.TapAsync(p.Item1, y, token);
                //}
                return;
            }

            if (step.Act == "rightclick")
            {
                //if (clickType == 2)
                //{
                //    var p = MouseHelper.WithRandom(step.PosX, step.PosY, step.RandX, step.RandY);
                //    int y = p.Item2 + yOffset;
                //    onStatus($"RIGHT CLICK ({p.Item1},{y}):{randSleep}(s)", Color.Red);
                //    await MouseHelper.RightClick(p.Item1, y);
                //}
                //else if (clickType == 1)
                //{
                //    var p = MouseHelper.WithRandom(step.ScrX, step.ScrY, step.RandX, step.RandY);
                //    int y = p.Item2 + yOffset;
                //    onStatus($"RIGHT CLICK ({p.Item1},{y}):{randSleep}(s)", Color.Red);
                //    await WinClick.RightClickScrcpyAtScreenAsync(p.Item1, y);
                //}
                //else
                //{
                onStatus("BACK BUTTON CLICK", Color.Red);
                await adb.KeyAsync(AndroidKeys.BACK, token);
                //}
                return;
            }

            if (step.Act == "leftdrag" || step.Act == "rightdrag" || step.Act == "updrag" || step.Act == "downdrag")
            {
                //if (clickType == 2)
                //{
                //    var p1 = MouseHelper.WithRandomDrag(step.PosX, step.PosY, step.RandX, step.RandY, false);
                //    var p2 = MouseHelper.WithRandomDrag(step.PosX2 ?? step.PosX, step.PosY2 ?? step.PosY, step.RandX, step.RandY, false);
                //    int y1 = p1.Item2 + yOffset;
                //    int y2 = p2.Item2 + yOffset;
                //    onStatus($"DRAG ({p1.Item1},{y1}):({p2.Item1},{y2}):{randSleep}(s)", Color.Red);
                //    await MouseHelper.LeftDragAsync(p1.Item1, y1, p2.Item1, y2, 1000);
                //}
                //else if (clickType == 1)
                //{
                //    var p1 = MouseHelper.WithRandomDrag(step.ScrX, step.ScrY, step.RandX, step.RandY, false);
                //    var p2 = MouseHelper.WithRandomDrag(step.ScrX2 ?? step.ScrX, step.ScrY2 ?? step.ScrY, step.RandX, step.RandY, false);
                //    int y1 = p1.Item2 + yOffset;
                //    int y2 = p2.Item2 + yOffset;
                //    onStatus($"DRAG ({p1.Item1},{y1}):({p2.Item1},{y2}):{randSleep}(s)", Color.Red);
                //    await WinClick.LeftDragScrcpyAtScreenAsync(p1.Item1, y1, p2.Item1, y2, 1000);
                //}
                //else
                //{
                var p1 = MouseHelper.WithRandomDrag(step.ScrX, step.ScrY, step.RandX, step.RandY, false);
                var p2 = MouseHelper.WithRandomDrag(step.ScrX2 ?? step.ScrX, step.ScrY2 ?? step.ScrY, step.RandX, step.RandY, false);
                int y1 = p1.Item2 + yOffset;
                int y2 = p2.Item2 + yOffset;
                onStatus($"DRAG ({p1.Item1},{y1}):({p2.Item1},{y2}):{randSleep}(s)", Color.Red);
                await adb.SwipeAsync(p1.Item1, y1, p2.Item1, y2, 150, token);
                //}
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
