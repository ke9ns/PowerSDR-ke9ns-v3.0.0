using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PowerSDR
{
    internal static class ShutdownTrace
    {
        private static readonly object sync = new object();
        internal static void Write(string directory, string message)
        {
            try
            {
                lock (sync) File.AppendAllText(Path.Combine(directory, "shutdown-map.log"),
                    DateTime.Now.ToString("o") + " " + message + Environment.NewLine);
            }
            catch (Exception ex) { Debug.WriteLine("Shutdown logging failed: " + ex); }
        }
    }

    public partial class SpotControl
    {
        private readonly MapWorkerLifetime mapWorkers = new MapWorkerLifetime();
        internal bool MapWorkersIdle { get { return mapWorkers.IsIdle; } }
        internal string PendingMapWorkers { get { return mapWorkers.Pending; } }

        internal void StopMapWorkers()
        {
            mapWorkers.Stop();
            VOATHREAD1 = true;
            SP5_Active = 0;
        }

        private bool StartMapWorker(string name, ThreadStart action, ThreadPriority priority)
        {
            return mapWorkers.TryStart(name, action, priority,
                delegate(Exception ex) { ShutdownTrace.Write(console.AppDataPath, name + " failed: " + ex); });
        }

        private void RunTrackSun()
        {
            try { TrackSun(); }
            finally { TrackOFF = true; }
        }

        private void RunVoacap()
        {
            try { VOACAP(); }
            finally { VOATHREAD = false; VOARUN = false; }
        }

        // Publish only on the UI thread, after finishing the Graphics object.
        // Queued publications are ignored once shutdown starts.
        private void PublishTrackedMap(Image map)
        {
            if (mapWorkers.Stopping) return;
            MethodInvoker publish = delegate
            {
                if (mapWorkers.Stopping || console.IsDisposed || console.Disposing ||
                    console.picDisplay.IsDisposed) return;
                console.picDisplay.SizeMode = PictureBoxSizeMode.StretchImage;
                console.picDisplay.BackgroundImage = map;
            };
            if (console.InvokeRequired)
            {
                try { console.BeginInvoke(publish); }
                catch (InvalidOperationException ex)
                { ShutdownTrace.Write(console.AppDataPath, "Map publication skipped: " + ex); }
            }
            else publish();
        }
    }

    sealed unsafe public partial class Console
    {
        private System.Windows.Forms.Timer mapCloseTimer;
        private Stopwatch mapCloseWait;

        // Defer closing while pumping normal window messages. On timeout cancel
        // the close, NOT the worker: no Thread.Abort, DoEvents or unsafe disposal.
        private bool DeferCloseForMap(FormClosingEventArgs e)
        {
            if (SpotForm == null) return false;
            SpotForm.StopMapWorkers();
            if (SpotForm.MapWorkersIdle)
            {
                if (mapCloseTimer != null)
                { mapCloseTimer.Stop(); mapCloseTimer.Dispose(); mapCloseTimer = null; }
                return false;
            }
            e.Cancel = true;
            if (mapCloseTimer != null) return true;
            ShutdownTrace.Write(app_data_path, "Waiting for map workers: " + SpotForm.PendingMapWorkers);
            mapCloseWait = Stopwatch.StartNew();
            mapCloseTimer = new System.Windows.Forms.Timer();
            mapCloseTimer.Interval = 100;
            mapCloseTimer.Tick += delegate
            {
                bool idle = SpotForm.MapWorkersIdle;
                if (!idle && mapCloseWait.ElapsedMilliseconds < 15000) return;
                mapCloseTimer.Stop();
                mapCloseTimer.Dispose();
                mapCloseTimer = null;
                if (idle)
                {
                    ShutdownTrace.Write(app_data_path, "Map workers stopped; resuming close.");
                    Close();
                }
                else
                {
                    ShutdownTrace.Write(app_data_path, "Close cancelled after 15s; pending: " + SpotForm.PendingMapWorkers);
                    MessageBox.Show(this,
                        "The map is still finishing a background operation. PowerSDR has not closed. " +
                        "Please wait and try closing again. Details: shutdown-map.log",
                        "Map shutdown pending", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            mapCloseTimer.Start();
            return true;
        }

        private void SaveShutdownCheckpoint()
        {
            try
            {
                if (setupForm != null && SaveTXProfileOnExit) setupForm.SaveTXProfileData();
                // State/options and child FormClosing handlers have finished.
                // Keep ds alive for ExitConsole and factory-reset handling.
                DB.WriteDatabase();
                ShutdownTrace.Write(app_data_path, "Database checkpoint saved before visual disposal: " + db_file_name);
            }
            catch (Exception ex)
            {
                ShutdownTrace.Write(app_data_path, "Database checkpoint FAILED: " + ex);
                MessageBox.Show(this, "The shutdown database checkpoint could not be saved. " +
                    "The normal shutdown save will still be attempted.\n\n" + ex.Message,
                    "Database save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
