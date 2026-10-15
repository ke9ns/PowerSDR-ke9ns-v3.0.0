using System;
using System.Drawing;
using System.Windows.Forms;
namespace PowerSDR
{
    public enum DSPMode { CWL, CWU, USB }
    public class Console { public DSPMode RX1DSPMode; public bool MOX; }
    internal static class Audio { internal static int SampleRate1 = 48000; }
    internal static class DspBackend { internal static Action<IntPtr,IntPtr,int,int> CwAudioSink; }
    internal class NeuralCwDecoder : IDisposable
    {
        internal NeuralCwDecoder(int rate, double gate) { KeyingGate = gate; }
        internal double KeyingGate, KeyingRatio, ToneHz;
        internal bool HasFullWindow;
        internal string Error;
        internal event Action<string> TextReceived;
        internal void Start() { }
        internal void Capture(IntPtr l, IntPtr r, int n, int rate) { }
        public void Dispose() { }
    }
    public partial class CWX : Form
    {
        private Console console = new Console();
        private Label label4 = new Label(), label5 = new Label(), label6 = new Label(), label7 = new Label();
        [STAThread] static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            using (var form = new CWX())
            {
                // Reproduce the real KE9NS designer constraints, not a free-size Form.
                form.FormBorderStyle = FormBorderStyle.Fixed3D;
                form.ClientSize = new Size(725, 328);
                form.MaximumSize = new Size(745, 371);
                form.MinimumSize = new Size(745, 371);
                form.InitializeNeuralCwPanel();
                form.ShowInTaskbar = false;
                form.Opacity = 0;
                form.Show();
                Application.DoEvents(); // Test harness only: realize controls before rendering.
                if (form.cwNeuralText.Bottom > 120) throw new Exception("Text outside panel");
                foreach (Control panel in form.Controls)
                {
                    if (panel.Bottom > form.ClientSize.Height || panel.Right > form.ClientSize.Width) throw new Exception("Panel outside form");
                    using (var bitmap = new Bitmap(panel.Width, panel.Height))
                    { panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, panel.Size)); bitmap.Save(args[0]); }
                }
                form.DisposeNeuralCwPanel();
            }
            System.Console.WriteLine("PASS: production CWX neural panel layout (hardware/decoder stubbed).");
            return 0;
        }
    }
}
