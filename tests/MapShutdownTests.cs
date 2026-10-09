using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PowerSDR
{
    // Real lifetime manager, WinForms message pump and production shutdown code.
    // Radio, DSP, and database are stubs; no user profile is opened.
    public partial class SpotControl : Form
    {
        private Console console;
        internal static bool VOATHREAD1, VOATHREAD;
        internal static int SP5_Active;
        internal bool TrackOFF, VOARUN;
        internal SpotControl(Console value) { console = value; }
        private void TrackSun() { }
        private void VOACAP() { throw new InvalidOperationException("synthetic VOACAP failure"); }
        internal bool Start(string name, ThreadStart work) { return StartMapWorker(name, work, ThreadPriority.Normal); }
        internal void Publish(Image image) { PublishTrackedMap(image); }
        internal void FailVoacap() { RunVoacap(); }
    }
    internal sealed class TestSetup
    {
        internal int Saves;
        internal void SaveTXProfileData() { Saves++; }
    }
    internal static class DB
    {
        internal static int Writes;
        internal static void WriteDatabase() { Writes++; }
    }
    sealed unsafe public partial class Console : Form
    {
        internal readonly PictureBox picDisplay = new PictureBox();
        internal SpotControl SpotForm;
        internal TestSetup setupForm = new TestSetup();
        internal bool SaveTXProfileOnExit = true;
        private string app_data_path, db_file_name = "synthetic-only.xml";
        internal string AppDataPath { get { return app_data_path; } }
        internal Console(string directory)
        {
            app_data_path = directory;
            SpotForm = new SpotControl(this);
            Controls.Add(picDisplay);
            ShowInTaskbar = false;
            Opacity = 0;
            FormClosing += delegate(object s, FormClosingEventArgs e)
            { if (!DeferCloseForMap(e)) SaveShutdownCheckpoint(); };
        }
        internal bool Waiting { get { return mapCloseTimer != null; } }
    }
    internal static class MapShutdownTests
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static void PumpUntil(Func<bool> done)
        {
            Stopwatch time = Stopwatch.StartNew();
            while (!done() && time.ElapsedMilliseconds < 5000)
            { Application.DoEvents(); Thread.Sleep(5); }
            Check(done(), "Timed out waiting for test completion");
        }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                string directory = Path.Combine(Path.GetFullPath(args[0]), "map-test-logs");
                Directory.CreateDirectory(directory);
                for (int round = 0; round < 50; round++)
                {
                    MapWorkerLifetime gate = new MapWorkerLifetime();
                    using (ManualResetEvent entered = new ManualResetEvent(false))
                    using (ManualResetEvent release = new ManualResetEvent(false))
                    {
                        Check(gate.TryStart("map", delegate { entered.Set(); release.WaitOne(); }, ThreadPriority.Normal, delegate { }), "Start");
                        Check(!gate.IsIdle, "Worker must be registered before Start returns");
                        Check(entered.WaitOne(2000), "Worker entered");
                        Check(!gate.TryStart("map", delegate { }, ThreadPriority.Normal, delegate { }), "Duplicate rejected");
                        gate.Stop();
                        Check(!gate.TryStart("voa", delegate { }, ThreadPriority.Normal, delegate { }), "Start after stop rejected");
                        Check(!gate.IsIdle, "Stop must not pretend a worker has finished");
                        release.Set();
                        PumpUntil(delegate { return gate.IsIdle; });
                    }
                }
                System.Console.WriteLine("PASS: 50 lifetime/register/duplicate/stop/drain cycles.");

                using (Console form = new Console(directory))
                using (ManualResetEvent queued = new ManualResetEvent(false))
                using (ManualResetEvent release = new ManualResetEvent(false))
                using (Bitmap map = new Bitmap(16, 16))
                {
                    form.Show();
                    int uiThread = Thread.CurrentThread.ManagedThreadId, publishedThread = 0;
                    form.picDisplay.BackgroundImageChanged += delegate { publishedThread = Thread.CurrentThread.ManagedThreadId; };
                    form.SpotForm.Start("publish", delegate { form.SpotForm.Publish(map); });
                    PumpUntil(delegate { return form.picDisplay.BackgroundImage == map; });
                    Check(publishedThread == uiThread, "Map must be published on UI thread");
                    PumpUntil(delegate { return form.SpotForm.MapWorkersIdle; });
                    form.picDisplay.BackgroundImage = null;
                    bool invoked = false;
                    form.SpotForm.Start("map", delegate
                    {
                        form.SpotForm.Publish(map);
                        queued.Set();
                        release.WaitOne();
                        form.Invoke((MethodInvoker)delegate { invoked = true; });
                    });
                    Check(queued.WaitOne(2000), "Map publication queued");
                    int writes = DB.Writes;
                    form.Close();
                    Check(!form.IsDisposed && form.Waiting, "Close must defer while worker owns controls");
                    Application.DoEvents();
                    Check(form.picDisplay.BackgroundImage == null, "Queued map must not publish after stop");
                    Check(DB.Writes == writes, "No final save while deferred");
                    release.Set();
                    PumpUntil(delegate { return form.IsDisposed; });
                    Check(invoked, "UI pump must service worker Invoke without deadlock");
                    Check(DB.Writes == writes + 1 && form.setupForm.Saves == 1, "Checkpoint on completed close");
                }
                System.Console.WriteLine("PASS: real UI publication, stale callback suppression, asynchronous close, synchronous Invoke and checkpoint.");

                using (Console form = new Console(directory))
                {
                    form.Show();
                    SpotControl.VOATHREAD = form.SpotForm.VOARUN = true;
                    form.SpotForm.Start("VOACAP failure", form.SpotForm.FailVoacap);
                    PumpUntil(delegate { return form.SpotForm.MapWorkersIdle; });
                    Check(!SpotControl.VOATHREAD && !form.SpotForm.VOARUN, "VOACAP flags reset on exception");
                    Check(File.ReadAllText(Path.Combine(directory,"shutdown-map.log")).Contains("synthetic VOACAP failure"), "Full exception logged");
                    form.Close();
                    Check(form.IsDisposed, "Idle map closes immediately");
                }
                System.Console.WriteLine("PASS: worker exception cleanup/logging and idle close. No radio or real database used.");
                return 0;
            }
            catch (Exception ex) { System.Console.WriteLine(ex); return 1; }
        }
    }
}
