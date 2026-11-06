using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
namespace PowerSDR
{
    internal static class ShutdownTrace { internal static void Write(string path, string text) { } }
    public partial class SpotControl : Form
    {
        internal Console console;
        private readonly MapWorkerLifetime mapWorkers = new MapWorkerLifetime();
        internal static int SP5_Active, Map_Last;
        internal CheckBox checkBoxMUF = new CheckBox(), chkVoacap = new CheckBox();
        internal TextBox statusBoxSWL = new TextBox(), textBox1 = new TextBox();
        partial void MapWorkerFailed(string name, Exception error);
        internal void FailWorker()
        {
            mapWorkers.TryStart("VOACAP", delegate { throw new WebException("synthetic disconnect"); },
                ThreadPriority.Normal, delegate(Exception ex) { MapWorkerFailed("VOACAP", ex); });
        }
        internal bool Idle { get { return mapWorkers.IsIdle; } }
        internal bool Blocked { get { return voacapNetworkBlocked; } }
    }
    sealed unsafe public partial class Console : Form
    {
        internal SpotControl SpotForm;
        internal bool VOAOFF;
        internal string AppDataPath { get { return "unused"; } }
        private void UpdateNoaaData() { throw new WebException("synthetic NOAA timeout", WebExceptionStatus.Timeout); }
    }
    internal static class VoacapNetworkSafetyTests
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static void Pump() { for(int i=0;i<30;i++) { Application.DoEvents(); Thread.Sleep(10); } }
        [STAThread] static int Main()
        {
            using(var console = new Console())
            using(var spot = new SpotControl())
            {
                console.SpotForm = spot; spot.console = console;
                var h=console.Handle; h=spot.Handle;
                spot.checkBoxMUF.Checked=spot.chkVoacap.Checked=true;
                spot.FailWorker(); Pump();
                Check(spot.Idle, "worker must drain after failure");
                Check(spot.Blocked && !spot.checkBoxMUF.Checked && !spot.chkVoacap.Checked && console.VOAOFF, "VOACAP must disable");
                Check(spot.statusBoxSWL.Text.Contains("VOACAP disabled"), "visible status missing");
                Check(!spot.IsDisposed && !console.IsDisposed, "application must remain open");
                spot.checkBoxMUF.Checked=spot.chkVoacap.Checked=true;
                Thread t=new Thread(console.NOAA); t.Start(); Check(t.Join(2000), "NOAA wrapper did not finish"); Pump();
                Check(!spot.checkBoxMUF.Checked, "NOAA failure must disable VOACAP");
                spot.Dispose();
                spot.DisableVoacapAfterFailure("late failure after disposal", new WebException());
            }
            System.Console.WriteLine("PASS: background exception contained, worker drains, VOACAP disabled on UI thread, visible status, NOAA failure and disposal race.");
            return 0;
        }
    }
}
