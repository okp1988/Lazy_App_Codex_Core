namespace Lazy_App_Codex_Core
{
    public static class MouseHelper
    {
        [ThreadStatic] private static Random _rng;
        private static Random Rng => _rng ?? (_rng = new Random(unchecked(Environment.TickCount * 397 ^ Thread.CurrentThread.ManagedThreadId)));

        public static (int x, int y) WithRandom(int baseX, int baseY, int randX, int randY)
            => (baseX + Rng.Next(-randX, randX + 1), baseY + Rng.Next(-randY, randY + 1));
    }
}
