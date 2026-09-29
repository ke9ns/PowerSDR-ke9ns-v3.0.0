using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSDR
{
    internal static class Direct2DSmokeTest
    {
        [STAThread]
        private static int Main()
        {
            using (Form form = new Form())
            using (Panel panel = new Panel())
            {
                form.ClientSize = new Size(640, 360);
                panel.Dock = DockStyle.Fill;
                form.Controls.Add(panel);
                form.CreateControl();
                panel.CreateControl();
                IntPtr handle = panel.Handle;
                bool initialized = Direct2DDisplay.TryInitialize(panel);
                bool nativeTraceQueued = false;
                bool nativeFillQueued = false;
                bool nativeWaterfallQueued = false;
                bool nativeLineQueued = false;
                Bitmap waterfall = new Bitmap(320, 120,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using (Graphics waterfallGraphics = Graphics.FromImage(waterfall))
                {
                    waterfallGraphics.Clear(Color.Navy);
                    waterfallGraphics.DrawLine(Pens.Yellow, 0, 0, 319, 119);
                }
                bool rendered = initialized && Direct2DDisplay.Render(panel, delegate(Graphics graphics)
                {
                    graphics.Clear(Color.Black);
                    Point[] trace = new Point[]
                    {
                        new Point(0, 359), new Point(160, 250),
                        new Point(320, 300), new Point(480, 120), new Point(639, 0)
                    };
                    nativeFillQueued = Direct2DDisplay.QueuePanadapterFill(
                        trace, trace.Length, 0, 359, Color.FromArgb(96, Color.Blue),
                        true, 128);
                    nativeTraceQueued = Direct2DDisplay.QueuePanadapterTrace(
                        trace, trace.Length, Color.Lime, 1.0f);
                    nativeWaterfallQueued = Direct2DDisplay.QueueWaterfallBitmap(
                        waterfall, 0, waterfall.Width, 200, 1, true);
                    nativeLineQueued = Direct2DDisplay.QueueLine(
                        0, 200, 319, 319, Color.White, 1.0f);
                    if (!nativeTraceQueued) graphics.DrawLines(Pens.Lime, trace);
                });
                Console.WriteLine("DIRECT2D_SUPPORTED=" + Direct2DDisplay.IsSupported());
                Console.WriteLine("DIRECT2D_INITIALIZED=" + initialized);
                Console.WriteLine("DIRECT2D_RENDERED=" + rendered);
                Console.WriteLine("DIRECT2D_NATIVE_TRACE=" + nativeTraceQueued);
                Console.WriteLine("DIRECT2D_NATIVE_FILL=" + nativeFillQueued);
                Console.WriteLine("DIRECT2D_NATIVE_WATERFALL=" + nativeWaterfallQueued);
                Console.WriteLine("DIRECT2D_NATIVE_LINE=" + nativeLineQueued);
                Console.WriteLine("DIRECT2D_ERROR=" + Direct2DDisplay.LastError);
                Direct2DDisplay.Shutdown();
                waterfall.Dispose();
                return initialized && rendered && nativeTraceQueued && nativeFillQueued &&
                    nativeWaterfallQueued && nativeLineQueued ? 0 : 1;
            }
        }
    }
}
