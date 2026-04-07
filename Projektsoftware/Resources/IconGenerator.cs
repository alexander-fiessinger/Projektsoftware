using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace Projektsoftware.Resources
{
    public static class IconGenerator
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
        {
            float d = r * 2;
            var path = new GraphicsPath();
            path.AddArc(x,         y,         d, d, 180, 90);
            path.AddArc(x + w - d, y,         d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
            path.AddArc(x,         y + h - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Render a bitmap at any size ───────────────────────────────────────

        private static Bitmap RenderBitmap(int size)
        {
            float sc = size / 256f;   // uniform scale factor
            float s  = size;

            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            // ── 1. Background: gradient rounded square ────────────────────────
            float pad   = 10 * sc;
            float bgRad = 48 * sc;
            var   bgR   = new RectangleF(pad, pad, s - 2 * pad, s - 2 * pad);

            using (var bgPath = RoundRect(pad, pad, s - 2 * pad, s - 2 * pad, bgRad))
            {
                // Soft drop shadow
                using (var sp = RoundRect(pad + 4 * sc, pad + 5 * sc, s - 2 * pad, s - 2 * pad, bgRad))
                using (var sb = new SolidBrush(Color.FromArgb(35, 0, 0, 80)))
                    g.FillPath(sb, sp);

                // Gradient: indigo-500 → blue-700
                using var grad = new LinearGradientBrush(bgR,
                    Color.FromArgb(99, 102, 241),
                    Color.FromArgb(37,  99, 235),
                    LinearGradientMode.ForwardDiagonal);
                g.FillPath(grad, bgPath);

                // Top-left glass shimmer
                g.SetClip(bgPath);
                using var shimmer = new LinearGradientBrush(
                    new RectangleF(0, 0, s, s * 0.45f),
                    Color.FromArgb(55, 255, 255, 255),
                    Color.FromArgb(0,  255, 255, 255),
                    LinearGradientMode.Vertical);
                g.FillRectangle(shimmer, new RectangleF(0, 0, s, s * 0.45f));
                g.ResetClip();
            }

            // For very small sizes: just a checkmark, skip all detail
            if (size <= 20)
            {
                float pw = Math.Max(1.5f, 2f * sc);
                using var wp = new Pen(Color.White, pw)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(wp, new[]
                {
                    new PointF(s * 0.28f, s * 0.52f),
                    new PointF(s * 0.44f, s * 0.70f),
                    new PointF(s * 0.72f, s * 0.30f)
                });
                return bmp;
            }

            // ── 2. Clipboard body ─────────────────────────────────────────────
            float cx = 62 * sc, cy = 50 * sc, cw = 132 * sc, ch = 148 * sc;
            float cbRad = 12 * sc;

            // Shadow
            using (var csp = RoundRect(cx + 4 * sc, cy + 5 * sc, cw, ch, cbRad))
            using (var csb = new SolidBrush(Color.FromArgb(35, 0, 0, 100)))
                g.FillPath(csb, csp);

            // White body
            using (var cbp = RoundRect(cx, cy, cw, ch, cbRad))
            using (var cbb = new SolidBrush(Color.White))
                g.FillPath(cbb, cbp);

            // Header strip (clipped to clipboard shape)
            using (var cbp = RoundRect(cx, cy, cw, ch, cbRad))
            {
                g.SetClip(cbp);
                using var hb = new SolidBrush(Color.FromArgb(232, 236, 252));
                g.FillRectangle(hb, cx, cy, cw, 28 * sc);
                g.ResetClip();
            }

            // Header title bar
            if (size >= 48)
            {
                using var htp = RoundRect(cx + 12 * sc, cy + 9 * sc, 65 * sc, 8 * sc, 4 * sc);
                using var htb = new SolidBrush(Color.FromArgb(160, 99, 102, 241));
                g.FillPath(htb, htp);
            }

            // Clip notch at top
            float nw = 46 * sc, nh = 14 * sc;
            float nx = cx + (cw - nw) / 2f, ny = cy - 7 * sc;
            using (var np = RoundRect(nx, ny, nw, nh, 6 * sc))
            using (var nb = new SolidBrush(Color.FromArgb(210, 215, 240)))
                g.FillPath(nb, np);

            // ── 3. Task rows ──────────────────────────────────────────────────
            if (size >= 48)
            {
                int rows = size >= 80 ? 4 : 3;
                float lr  = cx + 14 * sc;
                float ds  = 11 * sc;
                float bh  =  9 * sc;
                float[] ry = { cy + 38 * sc, cy + 62 * sc, cy + 86 * sc, cy + 110 * sc };
                float[] bw = { 78 * sc, 60 * sc, 72 * sc, 44 * sc };
                bool[]  done = { true, true, false, false };

                for (int i = 0; i < rows; i++)
                {
                    Color dc = done[i]
                        ? Color.FromArgb(52, 211, 153)
                        : Color.FromArgb(185, 190, 215);
                    int ba = done[i] ? 190 : 90;

                    using var db = new SolidBrush(dc);
                    g.FillEllipse(db, lr, ry[i] + sc, ds, ds);

                    using var bp = RoundRect(lr + ds + 7 * sc, ry[i] + sc, bw[i], bh, 4 * sc);
                    using var bb = new SolidBrush(Color.FromArgb(ba, 99, 102, 241));
                    g.FillPath(bb, bp);
                }
            }

            // ── 4. Checkmark badge (bottom-right) ─────────────────────────────
            float bx = 184 * sc, by = 186 * sc, br = 30 * sc;

            using (var bsh = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                g.FillEllipse(bsh, bx - br + 2, by - br + 3, br * 2, br * 2);

            using (var bf = new SolidBrush(Color.FromArgb(52, 211, 153)))   // emerald-400
                g.FillEllipse(bf, bx - br, by - br, br * 2, br * 2);

            using (var bbord = new Pen(Color.White, Math.Max(1.5f, 2.5f * sc)))
                g.DrawEllipse(bbord, bx - br, by - br, br * 2, br * 2);

            using var ckp = new Pen(Color.White, Math.Max(2.5f, 5.5f * sc))
                { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(ckp, new[]
            {
                new PointF(bx - 13 * sc, by + 2  * sc),
                new PointF(bx -  3 * sc, by + 12 * sc),
                new PointF(bx + 14 * sc, by - 10 * sc)
            });

            return bmp;
        }

        // ── Multi-size ICO writer ─────────────────────────────────────────────

        private static void WriteMultiSizeIco(string filePath, int[] sizes)
        {
            var pngData = sizes.Select(sz =>
            {
                using var bmp = RenderBitmap(sz);
                using var ms  = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }).ToArray();

            using var stream = new FileStream(filePath, FileMode.Create);
            using var w      = new BinaryWriter(stream);

            // ICONDIR header
            w.Write((short)0);
            w.Write((short)1);
            w.Write((short)sizes.Length);

            // Precalculate offsets
            int dataOffset = 6 + 16 * sizes.Length;
            int[] offsets  = new int[sizes.Length];
            offsets[0]     = dataOffset;
            for (int i = 1; i < sizes.Length; i++)
                offsets[i] = offsets[i - 1] + pngData[i - 1].Length;

            // ICONDIRENTRY per image
            for (int i = 0; i < sizes.Length; i++)
            {
                int sz = sizes[i];
                w.Write((byte)(sz == 256 ? 0 : sz)); // width  (0 = 256)
                w.Write((byte)(sz == 256 ? 0 : sz)); // height (0 = 256)
                w.Write((byte)0);                     // color count
                w.Write((byte)0);                     // reserved
                w.Write((short)1);                    // color planes
                w.Write((short)32);                   // bits per pixel
                w.Write(pngData[i].Length);           // data size
                w.Write(offsets[i]);                  // data offset
            }

            foreach (var png in pngData)
                w.Write(png);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static Icon GenerateApplicationIcon()
        {
            using var bmp   = RenderBitmap(256);
            IntPtr   hIcon  = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        public static void SaveIconToFile(string path)
        {
            WriteMultiSizeIco(path, new[] { 256, 48, 32, 16 });
        }
    }
}
