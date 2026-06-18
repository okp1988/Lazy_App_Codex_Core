namespace Lazy_App_Codex_Core
{
    internal sealed class CountdownProgressControl : Control
    {
        private double _progress;
        private string _caption = "--";
        private bool _active;
        private int _filledBlocks;
        private int _totalBlocks;

        public CountdownProgressControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            MinimumSize = new Size(120, 18);
        }

        public void SetState(double progress, string caption, bool active, int filledBlocks = 0, int totalBlocks = 0)
        {
            progress = Math.Clamp(progress, 0D, 1D);
            filledBlocks = Math.Max(0, filledBlocks);
            totalBlocks = Math.Max(0, totalBlocks);
            if (Math.Abs(_progress - progress) < 0.001D &&
                string.Equals(_caption, caption, StringComparison.Ordinal) &&
                _active == active &&
                _filledBlocks == filledBlocks &&
                _totalBlocks == totalBlocks)
            {
                return;
            }

            _progress = progress;
            _caption = caption;
            _active = active;
            _filledBlocks = filledBlocks;
            _totalBlocks = totalBlocks;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var background = new SolidBrush(_active ? Color.FromArgb(246, 248, 250) : SystemColors.Control);
            using var border = new Pen(Color.FromArgb(185, 190, 196));
            e.Graphics.FillRectangle(background, bounds);

            int innerWidth = Math.Max(1, Width - 2);
            int innerHeight = Math.Max(1, Height - 2);
            using var brush = new SolidBrush(Color.FromArgb(82, 168, 109));
            if (_active && _totalBlocks > 1)
            {
                int filledBlocks = Math.Min(_filledBlocks, _totalBlocks);
                for (int block = 0; block < filledBlocks; block++)
                {
                    int left = 1 + (int)Math.Round(block * innerWidth / (double)_totalBlocks);
                    int right = 1 + (int)Math.Round((block + 1) * innerWidth / (double)_totalBlocks);
                    int blockWidth = Math.Max(1, right - left);
                    if (blockWidth > 3 && block < _totalBlocks - 1)
                    {
                        blockWidth--;
                    }

                    e.Graphics.FillRectangle(brush, new Rectangle(left, 1, blockWidth, innerHeight));
                }
            }
            else
            {
                int fillWidth = _active && _progress > 0D
                    ? Math.Max(1, (int)Math.Round(innerWidth * _progress))
                    : 0;
                if (fillWidth > 0)
                {
                    Rectangle fill = new Rectangle(1, 1, fillWidth, innerHeight);
                    e.Graphics.FillRectangle(brush, fill);
                }
            }

            e.Graphics.DrawRectangle(border, bounds);
            DrawCaptionBackground(e.Graphics, bounds, innerWidth, innerHeight);

            TextRenderer.DrawText(
                e.Graphics,
                _caption,
                Font,
                bounds,
                _active ? Color.FromArgb(28, 32, 36) : SystemColors.ControlDarkDark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawCaptionBackground(Graphics graphics, Rectangle bounds, int innerWidth, int innerHeight)
        {
            if (!_active || string.IsNullOrWhiteSpace(_caption))
            {
                return;
            }

            Size textSize = TextRenderer.MeasureText(
                graphics,
                _caption,
                Font,
                bounds.Size,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int backingWidth = Math.Min(innerWidth, textSize.Width + 16);
            int backingHeight = Math.Min(innerHeight, textSize.Height + 6);
            var backingBounds = new Rectangle(
                1 + Math.Max(0, (innerWidth - backingWidth) / 2),
                1 + Math.Max(0, (innerHeight - backingHeight) / 2),
                backingWidth,
                backingHeight);

            using var backingBrush = new SolidBrush(Color.FromArgb(185, 250, 252, 250));
            using var backingBorder = new Pen(Color.FromArgb(105, 190, 198, 190));
            graphics.FillRectangle(backingBrush, backingBounds);
            graphics.DrawRectangle(backingBorder, backingBounds);
        }
    }
}
