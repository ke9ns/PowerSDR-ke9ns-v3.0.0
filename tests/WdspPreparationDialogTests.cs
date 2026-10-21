using System;
using System.Threading;
using System.Windows.Forms;
namespace PowerSDR
{
    internal static class WdspPreparationDialogTests
    {
        [STAThread]
        static int Main()
        {
            try
            {
                Application.EnableVisualStyles();
                int ticks = 0, workerId = 0, uiId = Thread.CurrentThread.ManagedThreadId;
                bool closeAttempted = false, prematureClose = false;
                using (var timer = new System.Windows.Forms.Timer { Interval = 50 })
                {
                    timer.Tick += delegate {
                        ticks++;
                        if (!closeAttempted && Application.OpenForms.Count > 0)
                        {
                            closeAttempted = true;
                            var form = Application.OpenForms[0];
                            form.Close();
                            prematureClose = form.IsDisposed;
                        }
                    };
                    timer.Start();
                    WdspPreparationDialog.Run(null, delegate(Action<string> progress) {
                        workerId = Thread.CurrentThread.ManagedThreadId;
                        progress("Synthetic cold planning test");
                        Thread.Sleep(650);
                    });
                }
                if (ticks < 3 || workerId == uiId || !closeAttempted || prematureClose)
                    throw new Exception("UI pumping / worker isolation / close guard failed.");
                bool caught = false;
                try { WdspPreparationDialog.Run(null, delegate(Action<string> p) { throw new Exception("test failure"); }); }
                catch (InvalidOperationException ex) { caught = ex.InnerException.Message == "test failure"; }
                if (!caught) throw new Exception("Worker failure was not propagated.");
                WdspPreparationDialog.Run(null, delegate(Action<string> p) { p("Cached start"); });
                System.Console.WriteLine("PASS: responsive modal UI, worker isolation, close guard, error propagation and fast restart.");
                return 0;
            }
            catch (Exception ex) { System.Console.WriteLine(ex); return 1; }
        }
    }
}
