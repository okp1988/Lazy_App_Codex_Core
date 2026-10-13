using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Lazy_App_Codex_Core
{
    // Renders only the move, axis-line and circular-arc primitives in Assets/restore.svg.
    // This is not a general SVG loader and needs no external renderer or shared-folder access.
    internal static class RestoreIcon
    {
        public static Bitmap Create()
        {
            using var source = typeof(RestoreIcon).Assembly.GetManifestResourceStream("Lazy_App_Codex_Core.Assets.restore.svg")
                ?? throw new InvalidOperationException("Missing embedded Restore icon.");
            var svg = XDocument.Load(source).Root!;
            using var outline = new GraphicsPath();
            foreach (var element in svg.Elements())
            {
                AppendPath(outline, (string)element.Attribute("d")!);
            }

            var bitmap = new Bitmap(24, 24);
            try
            {
                using var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(SystemColors.ControlText, 2F)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                graphics.DrawPath(pen, outline);
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        private static void AppendPath(GraphicsPath path, string data)
        {
            var tokens = Regex.Matches(data, @"[a-zA-Z]|[-+]?(?:\d*\.\d+|\d+)").Select(m => m.Value).ToArray();
            int index = 0;
            PointF point = default;
            float Number() => float.Parse(tokens[index++], CultureInfo.InvariantCulture);
            while (index < tokens.Length)
            {
                char command = tokens[index++][0];
                switch (command)
                {
                    case 'M':
                        path.StartFigure();
                        point = new PointF(Number(), Number());
                        break;
                    case 'v':
                        var down = new PointF(point.X, point.Y + Number());
                        path.AddLine(point, down);
                        point = down;
                        break;
                    case 'h':
                        var across = new PointF(point.X + Number(), point.Y);
                        path.AddLine(point, across);
                        point = across;
                        break;
                    case 'a':
                        float radius = Number(), radiusY = Number(), rotation = Number();
                        bool large = Number() != 0, sweep = Number() != 0;
                        var end = new PointF(point.X + Number(), point.Y + Number());
                        if (radius <= 0 || radius != radiusY || rotation != 0 || point == end)
                            throw new InvalidOperationException("Unsupported Restore icon arc.");
                        AddCircularArc(path, point, end, radius, large, sweep);
                        point = end;
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported Restore icon path command.");
                }
            }
        }

        private static void AddCircularArc(GraphicsPath path, PointF start, PointF end, double radius, bool large, bool sweep)
        {
            double dx = (start.X - end.X) / 2D, dy = (start.Y - end.Y) / 2D;
            double distanceSquared = dx * dx + dy * dy;
            radius = Math.Max(radius, Math.Sqrt(distanceSquared));
            double factor = Math.Sqrt(Math.Max(0D, (radius * radius - distanceSquared) / distanceSquared));
            if (large == sweep) factor = -factor;
            double centerX = (start.X + end.X) / 2D + factor * dy;
            double centerY = (start.Y + end.Y) / 2D - factor * dx;
            double angle = Math.Atan2(start.Y - centerY, start.X - centerX) * 180D / Math.PI;
            double delta = Math.Atan2(end.Y - centerY, end.X - centerX) * 180D / Math.PI - angle;
            if (sweep && delta < 0) delta += 360D;
            if (!sweep && delta > 0) delta -= 360D;
            path.AddArc((float)(centerX - radius), (float)(centerY - radius), (float)(radius * 2), (float)(radius * 2),
                (float)angle, (float)delta);
        }
    }
}
