using System;
using System.Drawing;
using System.Windows.Forms;
namespace PowerSDR
{
    internal static class Direct2DStatusTests
    {
        [STAThread] static int Main()
        {
            using (Panel panel = new Panel { Width = 640, Height = 360 })
            {
                IntPtr handle = panel.Handle;
                if (!Direct2DDisplay.TryInitialize(panel) || Direct2DDisplay.HasPresented) return 1;
                if (!Direct2DDisplay.Render(panel, delegate(Graphics g) { g.Clear(Color.Black); }) ||
                    !Direct2DDisplay.HasPresented) return 2;
                bool failed = !Direct2DDisplay.Render(panel, delegate(Graphics g)
                { throw new InvalidOperationException("Intentional fallback test"); });
                if (!failed || Direct2DDisplay.IsActive || Direct2DDisplay.HasPresented ||
                    !Direct2DDisplay.LastError.Contains("Intentional fallback test")) return 3;
                if (!Direct2DDisplay.TryInitialize(panel) || Direct2DDisplay.HasPresented ||
                    Direct2DDisplay.LastError.Length != 0) return 4;
                Direct2DDisplay.Shutdown();
                System.Console.WriteLine("PASS: hardware ready, presented, fallback, recovery and shutdown states.");
                return 0;
            }
        }
    }
}
