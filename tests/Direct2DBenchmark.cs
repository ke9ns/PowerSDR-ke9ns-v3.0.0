using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace PowerSDR
{
    internal static class Direct2DBenchmark
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const int Frames = 240;

        [STAThread]
        private static int Main()
        {
            using (Panel panel = new Panel { Width = Width, Height = Height })
            using (Bitmap waterfall = new Bitmap(Width, Height / 2,
                PixelFormat.Format24bppRgb))
            using (Font font = new Font("Arial", 8.0f))
            {
                IntPtr handle = panel.Handle;
                if (!Direct2DDisplay.TryInitialize(panel)) return 2;
                Point[][] histories = new Point[16][];
                for (int h = 0; h < histories.Length; h++) histories[h] = new Point[Width];
                using (Graphics image = Graphics.FromImage(waterfall)) image.Clear(Color.Navy);

                Action<int> render = delegate(int frame)
                {
                    for (int h = 0; h < histories.Length; h++)
                        for (int x = 0; x < Width; x++) histories[h][x] =
                            new Point(x, 250 + h * 4 + (int)(30 * Math.Sin((x + frame) * 0.02)));

                    if (!Direct2DDisplay.Render(panel, delegate(Graphics graphics)
                    {
                        graphics.Clear(Color.Black);
                        for (int x = 0; x < Width; x += 64)
                            graphics.DrawLine(Pens.DimGray, x, 0, x, Height);
                        graphics.DrawString("14.200 MHz", font, Brushes.White, 8, 8);
                        Direct2DDisplay.QueueWaterfallBitmap(waterfall, 0, Width,
                            Height / 2, 1, (frame & 3) == 0);
                        Direct2DDisplay.QueuePanadapterFill(histories[0], Width,
                            0, Height / 2, Color.FromArgb(80, Color.Blue), true, 96);
                        for (int h = histories.Length - 1; h >= 0; h--)
                            Direct2DDisplay.QueuePanadapterTrace(histories[h], Width,
                                Color.FromArgb(255, 40 + h * 10, 255 - h * 8), 1.0f);
                        Direct2DDisplay.QueueLine(Width / 2, 0, Width / 2, Height,
                            Color.Yellow, 1.0f);
                    })) throw new InvalidOperationException(Direct2DDisplay.LastError);
                };

                for (int i = 0; i < 20; i++) render(i);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                int gen0 = GC.CollectionCount(0);
                Stopwatch timer = Stopwatch.StartNew();
                for (int i = 0; i < Frames; i++) render(i);
                timer.Stop();
                Console.WriteLine("DIRECT2D_BENCHMARK_FPS=" +
                    (Frames / timer.Elapsed.TotalSeconds).ToString("F2"));
                Console.WriteLine("DIRECT2D_BENCHMARK_GEN0=" +
                    (GC.CollectionCount(0) - gen0));
                Console.WriteLine("DIRECT2D_BENCHMARK_ERROR=" + Direct2DDisplay.LastError);
                Direct2DDisplay.Shutdown();
                return 0;
            }
        }
    }
}
