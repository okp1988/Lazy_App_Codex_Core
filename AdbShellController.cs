using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lazy_App_Codex_Core
{
    public static class AndroidKeys
    {
        public const int HOME = 3;
        public const int BACK = 4;
        public const int ENTER = 66;
        public const int DEL = 67;
        public const int APP_SWITCH = 187;
        public const int UP = 19;
        public const int DOWN = 20;
        public const int LEFT = 21;
        public const int RIGHT = 22;
    }

    /// <summary>
    /// ADB-based controller (device-pixel coordinates).
    /// Works while you focus other apps (e.g., YouTube).
    /// </summary>
    public sealed class AdbShellController
    {
        public string AdbPath { get; }
        public string DeviceSelector { get; } // e.g. "-s R9CXXXX" or empty for default device

        /// <param name="adbPath">Full path to adb.exe (default C:\adb\adb.exe)</param>
        /// <param name="deviceSerial">Optional device serial. If provided, will prepend "-s {serial}" to all calls.</param>
        public AdbShellController(string adbPath = @"C:\adb\adb.exe", string? deviceSerial = null)
        {
            AdbPath = adbPath ?? @"C:\adb\adb.exe";
            DeviceSelector = string.IsNullOrWhiteSpace(deviceSerial) ? "" : ("-s " + deviceSerial + " ");
        }

        // ---------- Public high-level APIs (device-pixel coordinates) ----------

        public Task<int> TapAsync(int x, int y, CancellationToken ct, int timeoutMs = 8000) =>
            RunAsync($"shell input tap {x} {y}", ct, timeoutMs);

        public Task<int> SwipeAsync(int x1, int y1, int x2, int y2, int durationMs, CancellationToken ct, int timeoutMs = 8000) =>
            RunAsync($"shell input swipe {x1} {y1} {x2} {y2} {durationMs}", ct, timeoutMs);

        /// <summary>Long press implemented via swipe from (x,y) to itself.</summary>
        public Task<int> LongPressAsync(int x, int y, int durationMs, CancellationToken ct, int timeoutMs = 8000) =>
            RunAsync($"shell input swipe {x} {y} {x} {y} {durationMs}", ct, timeoutMs);

        /// <summary>Android keyevent, e.g. BACK=4, HOME=3, ENTER=66.</summary>
        public Task<int> KeyAsync(int androidKeyCode, CancellationToken ct, int timeoutMs = 8000) =>
            RunAsync($"shell input keyevent {androidKeyCode}", ct, timeoutMs);

        /// <summary>
        /// Inject text (simple escaping). For secure fields (PIN/password) some apps may block; fall back to manual typing if needed.
        /// </summary>
        public Task<int> TextAsync(string text, CancellationToken ct, int timeoutMs = 12000) =>
            RunAsync($"shell input text {EscapeText(text)}", ct, timeoutMs);

        // ---------- Helpers / Queries ----------

        /// <summary>Returns (width,height) in physical device pixels (portrait basis).</summary>
        public async Task<(int width, int height)> GetDeviceSizeAsync(CancellationToken ct, int timeoutMs = 6000)
        {
            var (code, stdout, _) = await RunCaptureAsync("shell wm size", ct, timeoutMs).ConfigureAwait(false);
            if (code == 0 && TryParseWmSize(stdout, out var w, out var h)) return (w, h);

            // Fallback: dumpsys display (simple parse)
            var (code2, stdout2, _) = await RunCaptureAsync("shell dumpsys display | grep -E \"PhysicalDisplayInfo|DisplayDeviceInfo\"", ct, timeoutMs).ConfigureAwait(false);
            if (code2 == 0 && TryParseDumpsysSize(stdout2, out w, out h)) return (w, h);

            return (1080, 2400); // last resort
        }

        /// <summary>0=0°, 1=90° CW, 2=180°, 3=270° CW.</summary>
        public async Task<int> GetUserRotationAsync(CancellationToken ct, int timeoutMs = 4000)
        {
            var (code, stdout, _) = await RunCaptureAsync("shell settings get system user_rotation", ct, timeoutMs).ConfigureAwait(false);
            return (code == 0 && int.TryParse(stdout.Trim(), out var rot)) ? rot : 0;
        }

        /// <summary>Optional convenience to ensure Wi-Fi device is reachable.</summary>
        public Task<int> ConnectAsync(string ipPort, CancellationToken ct, int timeoutMs = 6000) =>
            RunAsync($"connect {ipPort}", ct, timeoutMs);

        /// <summary>Send raw ADB args, returns exit code.</summary>
        public Task<int> RunAsync(string args, CancellationToken ct, int timeoutMs = 10000) =>
            RunCoreAsync(args, ct, timeoutMs, capture: false).ContinueWith(t => t.Result.exitCode, TaskScheduler.Default);

        /// <summary>Send raw ADB args, captures stdout/stderr.</summary>
        public Task<(int exitCode, string stdout, string stderr)> RunCaptureAsync(string args, CancellationToken ct, int timeoutMs = 10000) =>
            RunCoreAsync(args, ct, timeoutMs, capture: true);

        // ---------- Private core ----------

        private async Task<(int exitCode, string stdout, string stderr)> RunCoreAsync(string args, CancellationToken ct, int timeoutMs, bool capture)
        {
            var psi = new ProcessStartInfo
            {
                FileName = AdbPath,
                Arguments = DeviceSelector + args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = capture,
                RedirectStandardError = capture,
            };

            if (capture)
            {
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }

            Process? p = null;
            CancellationTokenSource? timeoutCts = null;
            CancellationTokenSource? linked = null;

            string so = "", se = "";
            Task soTask = Task.CompletedTask, seTask = Task.CompletedTask;

            try
            {
                p = new Process { StartInfo = psi, EnableRaisingEvents = false };

                try
                {
                    if (!p.Start())
                        return (-1, "", "Failed to start adb process");
                }
                catch (Exception ex)
                {
                    return (-1, "", "Failed to start adb: " + ex.Message);
                }

                if (capture)
                {
                    // These are Task<string> in .NET Fx; keep them as Task to await later.
                    soTask = p.StandardOutput.ReadToEndAsync();
                    seTask = p.StandardError.ReadToEndAsync();
                }

                timeoutCts = new CancellationTokenSource(timeoutMs);
                linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                // Wait for exit without using the Exited event (prevents ExitCode access races)
                await Task.Run(() => { try { p.WaitForExit(); } catch { } }, linked.Token)
                          .ConfigureAwait(false);

                var code = -1;
                try { if (p != null) code = p.ExitCode; } catch { /* still -1 */ }

                if (capture)
                {
                    try { so = await (Task<string>)soTask; } catch { }
                    try { se = await (Task<string>)seTask; } catch { }
                }

                return (code, so ?? "", se ?? "");
            }
            catch (OperationCanceledException)
            {
                try { if (p != null && !p.HasExited) p.Kill(); } catch { }
                return (-1, so ?? "", "Canceled/Timed out");
            }
            finally
            {
                if (linked != null) linked.Dispose();
                if (timeoutCts != null) timeoutCts.Dispose();
                if (p != null) p.Dispose();
            }
        }

        private static bool TryParseWmSize(string s, out int w, out int h)
        {
            // Example: "Physical size: 1080x2400"
            w = h = 0;
            var idx = s.IndexOf("Physical size", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var line = s.Substring(idx);
            var colon = line.IndexOf(':');
            if (colon < 0) return false;
            var wh = line.Substring(colon + 1).Trim();
            var sp = wh.Split('x');
            if (sp.Length != 2) return false;
            return int.TryParse(sp[0].Trim(), out w) && int.TryParse(sp[1].Trim(), out h);
        }

        private static bool TryParseDumpsysSize(string s, out int w, out int h)
        {
            // Very loose parse for "... 1080 x 2400 ..." patterns
            w = h = 0;
            var i = s.IndexOf("x");
            if (i <= 0) return false;
            // attempt to scan neighbors
            for (int k = 0; k < s.Length; k++)
            {
                if (char.IsDigit(s[k]))
                {
                    int a = k;
                    while (a < s.Length && char.IsDigit(s[a])) a++;
                    if (a < s.Length && s[a] == 'x')
                    {
                        int b = a + 1;
                        int b2 = b;
                        while (b2 < s.Length && char.IsDigit(s[b2])) b2++;
                        if (int.TryParse(s.Substring(k, a - k), out w) &&
                            int.TryParse(s.Substring(b, b2 - b), out h))
                            return true;
                    }
                }
            }
            return false;
        }

        private static string EscapeText(string s)
        {
            // adb shell input text rules: spaces -> %s; escape a few characters
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length * 2);
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case ' ': sb.Append("%s"); break;
                    case '&': sb.Append("\\&"); break;
                    case '|': sb.Append("\\|"); break;
                    case '(': sb.Append("\\("); break;
                    case ')': sb.Append("\\)"); break;
                    case '<': sb.Append("\\<"); break;
                    case '>': sb.Append("\\>"); break;
                    case ';': sb.Append("\\;"); break;
                    case '\'': sb.Append("\\'"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }
    }
}
