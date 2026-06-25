namespace Lazy_App_Codex_Core
{
    internal static class AppLogger
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

        public static void Initialize()
        {
            Directory.CreateDirectory(LogDirectory);
            DeleteExpiredLogs();
        }

        public static void LogWarning(string message, Exception? exception = null)
        {
            Write("WARN", message, exception);
        }

        public static void LogError(string message, Exception? exception = null)
        {
            Write("ERROR", message, exception);
        }

        private static void DeleteExpiredLogs()
        {
            DateTime cutoff = DateTime.Now.Subtract(RetentionPeriod);

            foreach (string filePath in Directory.EnumerateFiles(LogDirectory, "*.log", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (File.GetLastWriteTime(filePath) < cutoff)
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Write("WARN", $"Failed to remove expired log file '{filePath}'.", ex);
                }
            }
        }

        private static void Write(string level, string message, Exception? exception)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                string logPath = Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
                string entry = FormatEntry(level, message, exception);

                lock (SyncRoot)
                {
                    File.AppendAllText(logPath, entry);
                }
            }
            catch
            {
                // Logging must never crash the app.
            }
        }

        private static string FormatEntry(string level, string message, Exception? exception)
        {
            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            if (exception != null)
            {
                entry += exception + Environment.NewLine;
            }

            return entry;
        }
    }
}
