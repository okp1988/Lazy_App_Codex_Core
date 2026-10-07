using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace Lazy_App_Codex_Core
{
    internal static class LayoutCheckRunner
    {
        internal static int Run()
        {
            var report = new CheckReport
            {
                StartedUtc = DateTimeOffset.UtcNow,
                Runtime = Environment.Version.ToString(),
                WindowsVersion = Environment.OSVersion.VersionString,
                BuildSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Program).Assembly.Location)))
            };
            try
            {
                ApplicationConfiguration.Initialize();
                // Never turn test errors into the normal app's modal error dialogs.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                var screens = Screen.AllScreens;
                foreach (var screen in screens)
                {
                    foreach (bool dual in new[] { false, true })
                    {
                        report.Cases.Add(CheckCase(screen, dual, null, screen.Bounds.Size));
                    }
                }

                // These are font/geometry stress tests, NOT a request to change Windows DPI.
                foreach (int nominalDpi in new[] { 96, 120 })
                {
                    foreach (var resolution in new[] { new Size(1920, 1200), new Size(1920, 1080) })
                    {
                        foreach (bool dual in new[] { false, true })
                        {
                            report.Cases.Add(CheckCase(Screen.PrimaryScreen ?? screens[0], dual, nominalDpi, resolution));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                report.Error = ex.ToString();
            }

            try
            {
                string directory = Path.Combine(AppContext.BaseDirectory, "layout-check");
                if (Directory.Exists(directory) && (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new IOException("The layout-check report folder must not be a redirected folder.");
                }

                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.json");
                using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                JsonSerializer.Serialize(output, report, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(path);
                return report.Passed ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Could not write layout-check results: " + ex.Message);
                return 2;
            }
        }

        private static CaseResult CheckCase(Screen screen, bool dual, int? nominalDpi, Size resolution)
        {
            var result = new CaseResult
            {
                Kind = nominalDpi.HasValue ? "synthetic-font-geometry" : "native-host-geometry",
                Screen = screen.DeviceName,
                NominalDpi = nominalDpi,
                Resolution = $"{resolution.Width}x{resolution.Height}",
                ActualScreenBounds = screen.Bounds.ToString(),
                ActualWorkingArea = screen.WorkingArea.ToString(),
                Panels = dual ? 2 : 1
            };
            var assignedFonts = new List<Font>();
            try
            {
                using var hostFont = new Font("Segoe UI", 9F);
                using var host = new Form
                {
                    AutoScaleMode = AutoScaleMode.None,
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    MaximizeBox = false,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = screen.WorkingArea.Location,
                    ClientSize = RunSetLayout.ClientSize(dual),
                    Font = hostFont
                };
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    Padding = new Padding(6, 6, 6, 2),
                    RowCount = 1
                };
                var first = new RunSetControl(true, true);
                var second = new RunSetControl(false, false) { Visible = dual };
                host.Controls.Add(layout);
                RunSetLayout.Configure(layout, first, second);
                RunSetLayout.SetSecondColumnWidths(layout, dual);
                // Create native controls without showing, activating, or capturing a window.
                EnsureHandles(host);
                result.EffectiveWindowDpi = host.DeviceDpi;
                result.FontScale = nominalDpi.HasValue ? nominalDpi.Value / (double)host.DeviceDpi : 1D;
                var sets = dual ? new[] { first, second } : new[] { first };
                if (nominalDpi.HasValue)
                {
                    foreach (var set in sets)
                    {
                        ApplyFontStress(set, result.FontScale, assignedFonts);
                    }
                }

                LayoutTree(host);
                Add(result, "composition", "client-size", host.ClientSize == RunSetLayout.ClientSize(dual), host.ClientSize.ToString());
                Add(result, "composition", "first-panel-position", first.Location == new Point(6, 6), first.Bounds.ToString());
                if (dual)
                {
                    Add(result, "composition", "second-panel-gap", second.Left - first.Right == RunSetLayout.Gap, second.Bounds.ToString());
                }

                bool fits = nominalDpi.HasValue
                    ? host.ClientSize.Width <= resolution.Width && host.ClientSize.Height <= resolution.Height
                    : screen.WorkingArea.Contains(new Rectangle(host.Location, host.Size));
                Add(result, "composition", nominalDpi.HasValue ? "synthetic-client-resolution-fit" : "native-window-working-area-fit", fits,
                    nominalDpi.HasValue ? "Client envelope only; excludes taskbar and simulated window chrome." : host.Bounds.ToString());

                foreach (string state in new[] { "idle", "running", "infinite" })
                {
                    foreach (var set in sets)
                    {
                        SetSampleState(set, state);
                    }

                    LayoutTree(host);
                    for (int index = 0; index < sets.Length; index++)
                    {
                        CheckPanel(result, sets[index], $"{state}/set-{index + 1}");
                    }
                }

                result.Font = first.CountBox.Font.ToString();
                result.ControlDpi = first.CountBox.DeviceDpi;
                result.PanelBounds = sets.Select(set => set.Bounds.ToString()).ToArray();
            }
            catch (Exception ex)
            {
                Add(result, "case", "exception", false, ex.ToString());
            }
            finally
            {
                foreach (var font in assignedFonts) font.Dispose();
            }

            return result;
        }

        private static void SetSampleState(RunSetControl set, string state)
        {
            bool running = state == "running";
            set.CountBox.Value = state == "infinite" ? 0 : 99;
            set.InfiniteBox.Checked = state == "infinite";
            set.CountBox.Enabled = !running;
            set.InfiniteBox.Enabled = !running;
            set.ScriptBox.Enabled = !running;
            set.OffsetBox.Enabled = !running;
            set.TagFilter.Enabled = !running;
            set.DeviceBox.Enabled = !running;
            set.RunButton.Text = running ? "Stop" : "Run";
            set.ScriptBox.SetItems(new object[] { "Layout sample" });
            set.ScriptBox.SelectedItem = "Layout sample";
            set.CurrentActionLabel.Text = "LEFT";
            set.StepLabel.Text = "99 / 99";
            set.CycleLabel.Text = "99 / 99";
            set.NextActionLabel.Text = "DELAY";
            set.NextAtLabel.Text = "23:59:59 (00:30)";
            set.EstimatedEndLabel.Text = "23:59:59 (01:00)";
            foreach (var chip in set.TimelineLabels) chip.Text = "DELAY";
            set.CountdownBar.SetState(running ? 0.5D : 0D, "Waiting 00:30", running, 3, 6);
        }

        private static void CheckPanel(CaseResult result, RunSetControl set, string state)
        {
            Add(result, state, "fixed-panel-size", set.Size == new Size(RunSetControl.FixedWidth, RunSetControl.FixedHeight), set.Size.ToString());
            Add(result, state, "panel-contained", set.Parent!.ClientRectangle.Contains(set.Bounds), set.Bounds.ToString());
            foreach (var control in Descendants(set))
            {
                // Parent is hidden: Control.Visible is false even for intended visible children.
                // Use known mode expectations instead of skipping every invisible control.
                if (!set.ShowSharedButtons && control.Name is "btnConfig" or "btnWirelessAdb") continue;
                if (control.Parent is NumericUpDown) continue; // Checked separately with native border tolerances.
                Add(result, state, $"contained/{ControlName(control)}", control.Parent!.ClientRectangle.Contains(control.Bounds), control.Bounds.ToString());
                if (control is Label label)
                {
                    Size preferred = label.GetPreferredSize(Size.Empty);
                    Add(result, state, $"label-height/{ControlName(label)}", preferred.Height <= label.Height,
                        $"required={preferred}, available={label.Size}");
                    bool fixedCaption = label.Name is "lblCurrentActionName" or "lblStepName" or "lblCycleName"
                        or "lblNextActionName" or "lblNextAtName" or "lblEstimatedEndName";
                    if (!label.AutoEllipsis || fixedCaption)
                    {
                        Add(result, state, $"label-width/{ControlName(label)}", preferred.Width <= label.Width,
                            $"required={preferred}, available={label.Size}");
                    }
                }
            }

            var count = set.CountBox;
            var editor = count.Controls.OfType<TextBox>().FirstOrDefault();
            Add(result, state, "native-number-editor-present", editor != null, count.GetType().Name);
            if (editor != null)
            {
                Size digits = TextRenderer.MeasureText("99", editor.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                Add(result, state, "number-digits-fit", editor.Width >= digits.Width + 2 && editor.Height >= editor.Font.Height,
                    $"99 width={digits.Width}, editor={editor.Bounds}, font height={editor.Font.Height}");
                Add(result, state, "number-text-centered", Math.Abs(editor.Top * 2 + editor.Height - count.ClientSize.Height) <= 1,
                    $"editor={editor.Bounds}, client={count.ClientSize}");
                var spinner = count.Controls.Cast<Control>().FirstOrDefault(control => control != editor);
                Add(result, state, "spinner-space", spinner != null && spinner.Width >= 12 && editor.Right <= spinner.Left,
                    spinner?.Bounds.ToString() ?? "missing");
            }

            var remainingLabel = count.Parent!.Controls.OfType<Label>().First(label => label.Name == "lblRemaining");
            Add(result, state, "remaining-separation", remainingLabel.Right <= count.Left && count.Right <= set.InfiniteBox.Left,
                $"label={remainingLabel.Bounds}, count={count.Bounds}, infinity={set.InfiniteBox.Bounds}");
            Size infinitySize = set.InfiniteBox.GetPreferredSize(Size.Empty);
            Add(result, state, "infinity-fit", infinitySize.Width <= set.InfiniteBox.Width && infinitySize.Height <= set.InfiniteBox.Height,
                $"required={infinitySize}, available={set.InfiniteBox.Size}");
            foreach (var button in new[] { set.RunButton, set.ConfigButton, set.WirelessAdbButton }.OfType<Button>())
            {
                Size preferred = button.GetPreferredSize(Size.Empty);
                Add(result, state, $"button-fit/{button.Name}", preferred.Width <= button.Width && preferred.Height <= button.Height,
                    $"required={preferred}, available={button.Size}");
            }

            foreach (var textBox in Descendants(set.ScriptBox).OfType<TextBox>())
            {
                Add(result, state, "selector-text-height", textBox.Height >= textBox.Font.Height,
                    $"editor={textBox.Size}, font height={textBox.Font.Height}");
            }

            foreach (var combo in new[] { set.OffsetBox, set.TagFilter, set.DeviceBox })
            {
                var info = new ComboBoxInfo { Size = Marshal.SizeOf<ComboBoxInfo>() };
                bool measured = GetComboBoxInfo(combo.Handle, ref info);
                int textHeight = info.Item.Bottom - info.Item.Top;
                Add(result, state, $"combo-text-height/{combo.Name}", measured && textHeight >= combo.Font.Height,
                    $"native item height={textHeight}, font height={combo.Font.Height}, measured={measured}");
            }

            var countdown = set.CountdownBar;
            int captionHeight = TextRenderer.MeasureText("Waiting 00:30", countdown.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
            Add(result, state, "countdown-text-height", countdown.Height - 2 >= captionHeight,
                $"interior height={countdown.Height - 2}, caption height={captionHeight}");

            var chips = set.TimelineLabels;
            Add(result, state, "six-timeline-chips", chips.Count == 6, chips.Count.ToString());
            for (int index = 0; index < chips.Count; index++)
            {
                Add(result, state, $"timeline-text-height/{index + 1}", chips[index].Height >= chips[index].Font.Height,
                    $"height={chips[index].Height}, font height={chips[index].Font.Height}");
                for (int other = index + 1; other < chips.Count; other++)
                {
                    Add(result, state, $"timeline-separated/{index + 1}-{other + 1}", !chips[index].Bounds.IntersectsWith(chips[other].Bounds), "chip bounds");
                }
            }
        }

        private static void ApplyFontStress(Control root, double scale, List<Font> fonts)
        {
            var original = new[] { root }.Concat(Descendants(root)).Select(control => (control, control.Font)).ToArray();
            foreach (var (control, font) in original)
            {
                var scaled = new Font(font.FontFamily, (float)(font.Size * scale), font.Style, font.Unit);
                fonts.Add(scaled);
                control.Font = scaled;
            }
        }

        private static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }

        private static void EnsureHandles(Control root)
        {
            _ = root.Handle;
            foreach (Control child in root.Controls) EnsureHandles(child);
        }

        private static void LayoutTree(Control root)
        {
            root.PerformLayout();
            foreach (Control child in root.Controls) LayoutTree(child);
        }

        private static string ControlName(Control control) => string.IsNullOrEmpty(control.Name) ? control.GetType().Name : control.Name;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetComboBoxInfo(IntPtr handle, ref ComboBoxInfo info);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ComboBoxInfo
        {
            public int Size;
            public NativeRect Item, Button;
            public uint ButtonState;
            public IntPtr ComboHandle, ItemHandle, ListHandle;
        }

        private static void Add(CaseResult result, string state, string name, bool passed, string detail) =>
            result.Checks.Add(new CheckResult(state, name, passed, detail));

        private sealed record CheckResult(string State, string Name, bool Passed, string Detail);

        private sealed class CaseResult
        {
            public string Kind { get; init; } = "";
            public string Screen { get; init; } = "";
            public int? NominalDpi { get; init; }
            public string Resolution { get; init; } = "";
            public string ActualScreenBounds { get; init; } = "";
            public string ActualWorkingArea { get; init; } = "";
            public int Panels { get; init; }
            public int EffectiveWindowDpi { get; set; }
            public int ControlDpi { get; set; }
            public double FontScale { get; set; }
            public string Font { get; set; } = "";
            public string[] PanelBounds { get; set; } = Array.Empty<string>();
            public List<CheckResult> Checks { get; } = new();
            public bool Passed => Checks.Count > 0 && Checks.All(check => check.Passed);
        }

        private sealed class CheckReport
        {
            public DateTimeOffset StartedUtc { get; init; }
            public string Runtime { get; init; } = "";
            public string WindowsVersion { get; init; } = "";
            public string BuildSha256 { get; init; } = "";
            public string[] Limitations { get; } =
            {
                "Synthetic cases change fonts only; native Windows DPI/theme remains that of the host.",
                "Native cases inspect hidden native controls; no desktop screenshot or pixel comparison is performed.",
                "Intentionally ellipsized live values, long names and timeline text are not required to fit horizontally.",
                "Popup windows, Config/Pair/Track Touch, input behavior and real monitor DPI transitions are not checked.",
                "Both physical machines need their own native reports to establish actual 100% and 125% host coverage."
            };
            public string? Error { get; set; }
            public List<CaseResult> Cases { get; } = new();
            public bool Passed => Error == null && Cases.Count >= 8 && Cases.All(item => item.Passed);
        }
    }
}
