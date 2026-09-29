using System;
using System.Windows.Forms;

namespace PowerSDR
{
    public partial class Setup
    {
        private Timer rendererStatusTimer;

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (console == null || grpDisplayDriverEngine == null || IsDisposed) return;
            if (rendererStatusTimer == null)
            {
                rendererStatusTimer = new Timer(components);
                rendererStatusTimer.Interval = 1000;
                rendererStatusTimer.Tick += delegate { RefreshRendererDiagnostics(); };
            }
            rendererStatusTimer.Enabled = Visible;
            if (Visible) RefreshRendererDiagnostics();
        }

        private void RefreshRendererDiagnostics()
        {
            if (!Visible || IsDisposed) return;
            grpDisplayDriverEngine.Text = Direct2DDisplay.HasPresented ? "DX active" :
                Direct2DDisplay.IsActive ? "DX ready" :
                Direct2DDisplay.LastError.Length > 0 ? "DX fallback" : "GDI+ active";
            string diagnostics = console.DisplayDiagnostics;
            toolTip1.SetToolTip(comboDisplayDriver, diagnostics);
            toolTip1.SetToolTip(grpDisplayDriverEngine, diagnostics);
        }
    }
}
