using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ReSizeWindow
{
    // Tray icons are drawn at runtime (a window with a resize arrow) so no .ico asset is needed.
    internal sealed class TrayIcons : IDisposable
    {
        private static readonly Color ActiveColor = Color.FromArgb(59, 130, 246);
        private static readonly Color PausedColor = Color.FromArgb(140, 140, 140);
        private const float GridSize = 16f;

        private readonly Icon active = Create(ActiveColor);
        private readonly Icon paused = Create(PausedColor);

        public Icon For(bool enabled)
        {
            return enabled ? active : paused;
        }

        public void Dispose()
        {
            active.Dispose();
            paused.Dispose();
        }

        private static Icon Create(Color color)
        {
            int size = SystemInformation.SmallIconSize.Width;
            using (var bitmap = new Bitmap(size, size))
            {
                using (Graphics g = Graphics.FromImage(bitmap)) Draw(g, size / GridSize, color);

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon borrowed = Icon.FromHandle(handle)) return (Icon)borrowed.Clone();
                }
                finally
                {
                    Native.DestroyIcon(handle);
                }
            }
        }

        // Coordinates are on a 16x16 grid scaled by unit.
        private static void Draw(Graphics g, float unit, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(unit, unit);

            using (var pen = new Pen(color, 1.5f))
            using (var brush = new SolidBrush(color))
            {
                g.DrawRectangle(pen, 1.5f, 1.5f, 10f, 9f);
                g.FillRectangle(brush, 1f, 1f, 11f, 3.5f);

                g.DrawLine(pen, 8f, 8f, 13.5f, 13.5f);
                g.FillPolygon(brush, new[] { new PointF(15.5f, 15.5f), new PointF(15.5f, 10f), new PointF(10f, 15.5f) });
            }
        }
    }
}
