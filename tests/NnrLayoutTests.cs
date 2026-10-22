using System;
using System.Drawing;
using System.Windows.Forms;
namespace PowerSDR
{
    public class LabelTS : Label {} public class GroupBoxTS : GroupBox {}
    public class ButtonTS : Button {} public class NumericUpDownTS : NumericUpDown {}
    internal static class DspBackend
    {
        internal static double[][] Parameters = new double[2][];
        internal static void SetNnrParameters(int receiver, double[] values) { Parameters[receiver] = values; }
    }
    public partial class Setup : Form
    {
        private bool initializing;
        private ToolTip toolTip1 = new ToolTip();
        private TabControl tcSetup = new TabControl();
        private TabPage tpDSP = new TabPage("DSP");
        private TabControl tcDSP = new TabControl();
        private TabPage tpWdspNRs = new TabPage("NRs");
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static void CheckBounds(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (parent is GroupBox)
                    Check(c.Right <= parent.ClientSize.Width && c.Bottom <= parent.ClientSize.Height, "Clipped control " + c.Name);
                if (c is Label)
                    foreach (Control other in parent.Controls)
                        if (other is NumericUpDown) Check(!c.Bounds.IntersectsWith(other.Bounds), "Overlapping controls");
                CheckBounds(c);
            }
        }
        [STAThread] static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            using (var f = new Setup())
            {
                f.ClientSize = new Size(600, 390);
                f.tcSetup.Dock = DockStyle.Fill; f.Controls.Add(f.tcSetup);
                foreach (string s in new[] { "General", "Audio", "Display" }) f.tcSetup.TabPages.Add(s);
                f.tcSetup.TabPages.Add(f.tpDSP); f.tcSetup.TabPages.Add("Transmit"); f.tcSetup.TabPages.Add("PA"); f.tcSetup.TabPages.Add("Appearance");
                f.tcDSP.Dock = DockStyle.Fill; f.tpDSP.Controls.Add(f.tcDSP);
                foreach (string s in new[] { "Options", "Image Reject", "Keyer", "AGC/ALC" }) f.tcDSP.TabPages.Add(s);
                f.tcDSP.TabPages.Add(f.tpWdspNRs);
                f.InitializeNnrTab();
                f.ShowInTaskbar = false; f.Opacity = 0; f.Show(); Application.DoEvents();
                Check(f.tcSetup.TabPages.Count == 7, "No extra top-level tab");
                Check(f.tcDSP.TabPages[4].Text == "NRs" && f.tcDSP.TabPages[5].Text == "NNR", "New DSP tab order");
                f.SelectLegacySetupTab(4); Check(f.tcSetup.SelectedTab.Text == "Transmit", "Transmit shortcut");
                f.SelectLegacySetupTab(6); Check(f.tcSetup.SelectedTab.Text == "Appearance", "Appearance shortcut");
                f.SetNnrPreset(0, new decimal[] { -18,6,.75m,12,3,20,120 });
                Check(DspBackend.Parameters[0][0] == -18 && DspBackend.Parameters[1][0] == -25, "Independent settings");
                f.tcSetup.SelectedTab = f.tpDSP; f.tcDSP.SelectedIndex = 5;
                f.ShowInTaskbar = false; f.Opacity = 0; f.Show(); Application.DoEvents();
                CheckBounds(f);
                using (var b = new Bitmap(f.tcSetup.Width, f.tcSetup.Height))
                { f.tcSetup.DrawToBitmap(b, new Rectangle(Point.Empty, b.Size)); b.Save(args[0]); }
                f.Scale(new SizeF(1.5f, 1.5f)); f.PerformLayout(); CheckBounds(f);
            }
            System.Console.WriteLine("PASS: NNR tab order, legacy shortcuts, independent presets and 100/150% layout (hardware stubbed).");
            return 0;
        }
    }
}
