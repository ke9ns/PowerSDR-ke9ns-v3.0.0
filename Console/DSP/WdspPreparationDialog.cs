using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace PowerSDR
{
    // Keep the UI message loop alive while native FFT planning runs. Never abort
    // that worker or dispose its channels while FFTW owns them.
    internal sealed class WdspPreparationDialog : Form
    {
        internal const int DisplayDelayMilliseconds = 300;
        private readonly Action<Action<string>> prepare;
        private readonly Label status = new Label();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly Stopwatch elapsed = new Stopwatch();
        private volatile string stage = "Preparing WDSP...";
        private volatile bool finished;
        private Exception failure;

        internal WdspPreparationDialog(Action<Action<string>> prepare)
        {
            this.prepare = prepare;
            Text = "PowerSDR - Preparing DSP";
            ClientSize = new Size(510, 175);
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ControlBox = false;
            ShowInTaskbar = false;
            var explanation = new Label { Left = 18, Top = 16, Width = 474, Height = 62,
                Text = "Preparing FFT plans before starting the radio.\r\nThe first preparation may take several minutes.\r\nPlease wait; do not close PowerSDR." };
            status.SetBounds(18, 84, 474, 36);
            var progress = new ProgressBar { Left = 18, Top = 132, Width = 474, Height = 20,
                Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30 };
            Controls.AddRange(new Control[] { explanation, status, progress });
            timer.Interval = 100;
            timer.Tick += delegate {
                status.Text = stage + "\r\nElapsed: " + elapsed.Elapsed.ToString(@"mm\:ss");
                if (finished) { timer.Stop(); Close(); }
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            timer.Start();
        }

        private Thread StartPreparation()
        {
            elapsed.Start();
            var worker = new Thread(delegate() {
                try { prepare(delegate(string text) { stage = text; }); }
                catch (Exception ex) { failure = ex; }
                finally { finished = true; }
            });
            worker.IsBackground = true;
            worker.Name = "WDSP FFT preparation";
            worker.Start();
            return worker;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!finished) e.Cancel = true;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        internal static bool Run(IWin32Window owner, Action<Action<string>> prepare)
        {
            using (var dialog = new WdspPreparationDialog(prepare))
            {
                // Setup can restart audio with already prepared channels. Give
                // that fast path a short, bounded interval without showing a
                // window. Only real waits enter the responsive modal loop.
                Thread worker = dialog.StartPreparation();
                bool shown = !worker.Join(DisplayDelayMilliseconds);
                if (shown) dialog.ShowDialog(owner);
                if (dialog.failure != null)
                    throw new InvalidOperationException("WDSP preparation failed.", dialog.failure);
                return shown;
            }
        }
    }
}
