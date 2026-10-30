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
                    bool shown = WdspPreparationDialog.Run(null, delegate(Action<string> progress) {
                        workerId = Thread.CurrentThread.ManagedThreadId;
                        progress("Synthetic cold planning test");
                        Thread.Sleep(WdspPreparationDialog.DisplayDelayMilliseconds + 700);
                    });
                    if (!shown) throw new Exception("Slow preparation must show progress.");
                }
                if (ticks < 3 || workerId == uiId || !closeAttempted || prematureClose)
                    throw new Exception("UI pumping / worker isolation / close guard failed.");
                bool caught = false;
                try { WdspPreparationDialog.Run(null, delegate(Action<string> p) { throw new Exception("test failure"); }); }
                catch (InvalidOperationException ex) { caught = ex.InnerException.Message == "test failure"; }
                if (!caught) throw new Exception("Worker failure was not propagated.");
                for (int i = 0; i < 10; i++)
                {
                    bool shown = WdspPreparationDialog.Run(null, delegate(Action<string> p) { p("Cached start"); });
                    if (shown) throw new Exception("Cached restart must not flash a dialog.");
                }
                bool delayedFailure = false;
                try { WdspPreparationDialog.Run(null, delegate(Action<string> p) {
                    Thread.Sleep(WdspPreparationDialog.DisplayDelayMilliseconds + 300); throw new Exception("delayed failure");
                }); }
                catch (InvalidOperationException ex) { delayedFailure = ex.InnerException.Message == "delayed failure"; }
                if (!delayedFailure) throw new Exception("Delayed failure was not propagated.");
                System.Console.WriteLine("PASS: slow progress, responsive UI, close guard, immediate/delayed errors and 10 cached starts without dialog.");
                return 0;
            }
            catch (Exception ex) { System.Console.WriteLine(ex); return 1; }
        }
    }
}
