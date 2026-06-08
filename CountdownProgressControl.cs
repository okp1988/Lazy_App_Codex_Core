namespace Lazy_App_Codex_Core
{
    internal sealed class CountdownProgressControl : Control
    {
        private double _progress;
        private string _caption = "--";
        private bool _active;

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

        public void SetState(double progress, string caption, bool active)
        {
            progress = Math.Clamp(progress, 0D, 1D);
            if (Math.Abs(_progress - progress) < 0.001D &&
                string.Equals(_caption, caption, StringComparison.Ordinal) &&
                _active == active)
            {
                return;
            }

            _progress = progress;
            _caption = caption;
            _active = active;
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

            int fillWidth = _active ? (int)Math.Round((Width - 2) * _progress) : 0;
            if (fillWidth > 0)
            {
                Rectangle fill = new Rectangle(1, 1, fillWidth, Math.Max(1, Height - 2));
                using var brush = new SolidBrush(Color.FromArgb(82, 168, 109));
                e.Graphics.FillRectangle(brush, fill);
            }

            e.Graphics.DrawRectangle(border, bounds);

            TextRenderer.DrawText(
                e.Graphics,
                _caption,
                Font,
                bounds,
                _active ? Color.FromArgb(28, 32, 36) : SystemColors.ControlDarkDark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
