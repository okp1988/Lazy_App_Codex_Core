using System.Globalization;

namespace Lazy_App_Codex_Core
{
    /// <summary>Plain two-line preview; Text always retains the complete remark.</summary>
    internal sealed class RemarkTextControl : Control
    {
        internal const string OverflowMarker = ".....";
        private const TextFormatFlags DrawFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
            TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping;
        private string[] _lines = Array.Empty<string>();

        public RemarkTextControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Transparent;
            TabStop = false;
            AccessibleRole = AccessibleRole.StaticText;
        }

        internal int DisplayLineCount => _lines.Length;
        internal string DisplayText => string.Join(Environment.NewLine, _lines);
        internal bool IsTruncated { get; private set; }

        protected override void OnTextChanged(EventArgs e)
        {
            AccessibleDescription = Text;
            UpdatePreview();
            base.OnTextChanged(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdatePreview();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdatePreview();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            for (int row = 0; row < _lines.Length; row++)
            {
                TextRenderer.DrawText(e.Graphics, _lines[row], Font,
                    new Rectangle(0, row * Font.Height, ClientSize.Width, Font.Height), ForeColor, DrawFlags);
            }
        }

        private void UpdatePreview()
        {
            var lines = new List<string>(2);
            string source = Text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
            int cursor = 0;
            while (cursor < source.Length && lines.Count < 2 && ClientSize.Width > 0)
            {
                int end = source.IndexOf('\n', cursor);
                if (end < 0) end = source.Length;
                string paragraph = source[cursor..end];
                int fit = FitPrefix(paragraph, ClientSize.Width);
                if (fit == paragraph.Length)
                {
                    lines.Add(paragraph);
                    cursor = end < source.Length ? end + 1 : end;
                }
                else
                {
                    // Prefer a word boundary, but wrap long words/CJK at a text-element boundary.
                    int[] elements = StringInfo.ParseCombiningCharacters(paragraph[..fit]);
                    for (int index = elements.Length - 1; index >= 0; index--)
                    {
                        if (!char.IsWhiteSpace(paragraph, elements[index])) continue;
                        fit = index + 1 < elements.Length ? elements[index + 1] : fit;
                        break;
                    }
                    if (fit == 0) break; // A single text element is wider than the display.
                    lines.Add(paragraph[..fit].TrimEnd());
                    cursor += fit;
                    while (cursor < end && char.IsWhiteSpace(source, cursor))
                        cursor += StringInfo.GetNextTextElement(source, cursor).Length;
                    if (cursor == end && end < source.Length) cursor++;
                }
            }

            IsTruncated = cursor < source.Length;
            if (IsTruncated && ClientSize.Width > 0)
            {
                if (lines.Count == 0) lines.Add("");
                string last = lines[^1];
                string marker = OverflowMarker[..FitPrefix(OverflowMarker, ClientSize.Width)];
                int fit = FitPrefix(last, ClientSize.Width, marker);
                lines[^1] = last[..fit].TrimEnd() + marker;
            }

            _lines = lines.ToArray();
            Invalidate();
        }

        private int FitPrefix(string text, int width, string suffix = "")
        {
            if (text.Length == 0 || width <= 0) return 0;
            int[] elements = StringInfo.ParseCombiningCharacters(text);
            int low = 0, high = elements.Length;
            while (low < high)
            {
                int middle = low + (high - low + 1) / 2;
                int length = middle == elements.Length ? text.Length : elements[middle];
                string candidate = suffix.Length == 0 ? text[..length] : text[..length].TrimEnd() + suffix;
                if (Measure(candidate) <= width) low = middle;
                else high = middle - 1;
            }

            return low == elements.Length ? text.Length : elements[low];
        }

        private int Measure(string text) => text.Length == 0 ? 0 :
            TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, int.MaxValue), DrawFlags).Width;
    }
}
