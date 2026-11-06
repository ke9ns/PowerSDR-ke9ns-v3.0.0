using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

namespace PowerSDR
{
    public partial class SpotControl
    {
        //.10 Network/worker failures must disable VOACAP without terminating PowerSDR.
        private volatile bool voacapNetworkBlocked;
        private int voacapFailurePending;

        internal bool CheckVoacapNetwork()
        {
            try
            {
                // A connected adapter does not prove Internet access. Actual request
                // failures are handled separately; do not probe unrelated websites.
                if (NetworkInterface.GetIsNetworkAvailable()) return true;
                DisableVoacapAfterFailure("No network connection", null);
            }
            catch (NetworkInformationException ex)
            {
                DisableVoacapAfterFailure("Network status unavailable", ex);
            }
            return false;
        }

        partial void MapWorkerFailed(string name, Exception error)
        {
            if (name == "TrackSun") SP5_Active = 0;
            DisableVoacapAfterFailure(name + " operation failed", error);
        }

        internal void DisableVoacapAfterFailure(string reason, Exception error)
        {
            voacapNetworkBlocked = true;
            if (Interlocked.Exchange(ref voacapFailurePending, 1) != 0) return;
            ShutdownTrace.Write(console.AppDataPath, "VOACAP disabled: " + reason +
                (error == null ? "" : Environment.NewLine + error.ToString()));
            try
            {
                if (IsDisposed || Disposing || !IsHandleCreated || mapWorkers.Stopping)
                {
                    Interlocked.Exchange(ref voacapFailurePending, 0);
                    return;
                }
                // Always queue: this also avoids reentering CheckedChanged when
                // the user tries to enable VOACAP while offline.
                BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        if (IsDisposed || Disposing || mapWorkers.Stopping) return;
                        bool wasEnabled = checkBoxMUF.Checked || chkVoacap.Checked;
                        checkBoxMUF.Checked = false;
                        chkVoacap.Checked = false;
                        console.VOAOFF = true;
                        Map_Last |= 2;
                        if (wasEnabled)
                        {
                            const string message = "VOACAP disabled: map or space-weather data unavailable. " +
                                "Check the connection and enable VOACAP again. Details: shutdown-map.log";
                            statusBoxSWL.Text = message;
                            textBox1.AppendText(Environment.NewLine + message + Environment.NewLine);
                        }
                    }
                    finally { Interlocked.Exchange(ref voacapFailurePending, 0); }
                });
            }
            catch (InvalidOperationException)
            {
                // Window closed while the failure notification was being queued.
                Interlocked.Exchange(ref voacapFailurePending, 0);
            }
        }
    }

    sealed unsafe public partial class Console
    {
        //.10 NOAA is also a raw background-thread entry point outside the map worker.
        public void NOAA()
        {
            try
            {
                if (IsDisposed || Disposing) return;
                if (SpotForm != null && !SpotForm.CheckVoacapNetwork()) return;
                UpdateNoaaData();
            }
            catch (Exception ex) { ReportVoacapDataFailure(ex); }
        }

        private void ReportVoacapDataFailure(Exception error)
        {
            ShutdownTrace.Write(AppDataPath, "Space-weather update failed: " + error);
            if (SpotForm != null && !SpotForm.IsDisposed)
                SpotForm.DisableVoacapAfterFailure("Space-weather data unavailable", error);
        }
    }
}
