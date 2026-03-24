using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Projektsoftware.Resources
{
    public static class IconGenerator
    {
        public static Icon GenerateApplicationIcon()
        {
            int size = 256;
            using (Bitmap bmp = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Background Circle
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(25, 118, 210)))
                {
                    g.FillEllipse(brush, 8, 8, 240, 240);
                }

                // Inner Circle
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(13, 71, 161)))
                {
                    g.FillEllipse(brush, 28, 28, 200, 200);
                }

                // Folder
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    Point[] folderPoints = new Point[]
                    {
                        new Point(70, 90),
                        new Point(70, 180),
                        new Point(185, 180),
                        new Point(185, 100),
                        new Point(130, 100),
                        new Point(120, 85),
                        new Point(70, 85)
                    };
                    g.FillPolygon(brush, folderPoints);
                }

                // Document lines
                using (Pen pen = new Pen(Color.White, 6))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 85, 120, 165, 120);
                    g.DrawLine(pen, 85, 140, 145, 140);
                    g.DrawLine(pen, 85, 160, 155, 160);
                }

                // Checkmark
                using (Pen pen = new Pen(Color.FromArgb(76, 175, 80), 8))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 150, 155, 160, 168);
                    g.DrawLine(pen, 160, 168, 180, 135);
                }

                // Convert to Icon
                IntPtr hIcon = bmp.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);
                return icon;
            }
        }

        public static void SaveIconToFile(string path)
        {
            using (Icon icon = GenerateApplicationIcon())
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                icon.Save(fs);
            }
        }
    }
}
