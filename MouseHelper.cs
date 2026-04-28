using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lazy_App_Codex_Core
{
    public static class MouseHelper
    {
        [ThreadStatic] private static Random _rng;
        private static Random Rng => _rng ?? (_rng = new Random(unchecked(Environment.TickCount * 397 ^ Thread.CurrentThread.ManagedThreadId)));

        public static (int x, int y) WithRandom(int baseX, int baseY, int randX, int randY)
            => (baseX + Rng.Next(-randX, randX + 1), baseY + Rng.Next(-randY, randY + 1));

        public static (int x, int y) WithRandomDrag(int baseX, int baseY, int randX, int randY, bool isLeftDrag)
        {
            int offsetX;
            if (isLeftDrag)
            {
                // always to the left: negative X
                offsetX = -Rng.Next(0, randX + 1);
            }
            else
            {
                // always to the right: positive X
                offsetX = Rng.Next(0, randX + 1);
            }

            // Y still random up/down
            int offsetY = Rng.Next(-randY, randY + 1);

            return (baseX + offsetX, baseY + offsetY);
        }

    }
}
