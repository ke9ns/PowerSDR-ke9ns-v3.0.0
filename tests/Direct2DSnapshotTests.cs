using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using SharpDX.Mathematics.Interop;
namespace PowerSDR
{
    internal static class Direct2DSnapshotTests
    {
        [STAThread] static int Main()
        {
            using(var panel=new Panel { Width=640, Height=360 })
            {
                IntPtr handle=panel.Handle;
                if(!Direct2DDisplay.TryInitialize(panel)) return 1;
                Point[] points={new Point(5,10),new Point(20,30)};
                bool ok=Direct2DDisplay.Render(panel,delegate(Graphics g)
                {
                    Direct2DDisplay.QueuePanadapterTrace(points,2,Color.White,1);
                    points[0]=new Point(99,199);
                    Direct2DDisplay.QueuePanadapterFill(points,2,0,360,Color.Blue,false,255);
                    points[0]=new Point(300,310);
                    var commands=(IList)typeof(Direct2DDisplay).GetField("nativeCommands",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
                    object trace=commands[0], fill=commands[1];
                    var field=trace.GetType().GetField("Points",BindingFlags.Instance|BindingFlags.NonPublic);
                    RawVector2[] a=(RawVector2[])field.GetValue(trace), b=(RawVector2[])field.GetValue(fill);
                    if(a[0].X!=5.5f || a[0].Y!=10.5f || b[0].X!=99 || b[0].Y!=199 || Object.ReferenceEquals(a,b))
                        throw new Exception("Queued geometry changed with the legacy source buffer");
                });
                Console.WriteLine("SNAPSHOT_ISOLATION="+ok+" "+Direct2DDisplay.LastError);
                Direct2DDisplay.Shutdown(); return ok?0:1;
            }
        }
    }
}
