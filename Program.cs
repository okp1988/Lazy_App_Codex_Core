namespace Lazy_App_Codex_Core
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppLogger.Initialize();
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Fatal application error.", ex);
                MessageBox.Show("Unexpected error occurred. Please check the logs folder.", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLogger.LogError("Unhandled UI thread exception.", e.Exception);
            MessageBox.Show("Unexpected error occurred. Please check the logs folder.", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                AppLogger.LogError("Unhandled application exception.", ex);
                return;
            }

            AppLogger.LogError($"Unhandled application exception: {e.ExceptionObject}");
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.LogError("Unobserved task exception.", e.Exception);
            e.SetObserved();
        }
    }
}
