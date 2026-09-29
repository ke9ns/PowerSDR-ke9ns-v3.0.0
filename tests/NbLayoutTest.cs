using System;
using System.Drawing;
using System.Windows.Forms;
namespace PowerSDR {
 public enum DSPMode { FIRST=-1, LSB, USB }
 public enum AGCMode { FIRST=-1, FIXD, LONG, SLOW, MED, FAST }
 public class LabelTS:Label {} public class GroupBoxTS:GroupBox {}
 public class ComboBoxTS:ComboBox {} public class ButtonTS:Button {}
 public class NumericUpDownTS:NumericUpDown {} public class CheckBoxTS:CheckBox {}
 public class TextBoxTS:TextBox {}
 public static class DspBackend {
  public static bool ExperimentalWdspRequested=true;
  internal static WdspBlankerSettings Nb1Settings, Nb2Settings;
  public static void SetRx1NrParameters(params object[] values) {}
  public static void LoadRnnrModel(string p) {}
 }
 public partial class Setup:Form {
  bool initializing=true;
  ToolTip toolTip1=new ToolTip(); TabControl tcDSP=new TabControl();
  TabPage tpDSPOptions=new TabPage("Options");
  GroupBoxTS grpDSPLMSNR=NewGroup("NR",8,8,112,128), grpDSPNB=NewGroup("Noise Blanker",384,8,120,141),
   grpDSPNB2=NewGroup("Noise Blanker 2",385,155,120,53), grpDSPLMSANF=NewGroup("ANF",128,8,120,128),
   grpDSPBufferSize=NewGroup("Buffer Size",256,8,120,248), grpDSPWindow=NewGroup("Window",395,254,120,56);
  ButtonTS btnRSTNR=NewButton("Reset NR",44,142,68), btnRSTNB=NewButton("Reset NB",510,27,70),
   btnRSTNB2=NewButton("Reset NB2",510,89,70), btnRSTANF=NewButton("Reset ANF",164,142,68);
  LabelTS labelTS28=NewLabel("Legacy buffer note",24,171,208), labelTS7=NewLabel("TNF Notch Width\n(Default 100):",13,254,96);
  CheckBoxTS chkDSPRX2=new CheckBoxTS(), chkDSPTXMeterPeak=new CheckBoxTS();
  NumericUpDownTS udTNFWidth=new NumericUpDownTS();
  public Setup() {
   Font=new Font("Microsoft Sans Serif",8.25f);
   ClientSize=new Size(600,345); tcDSP.Dock=DockStyle.Fill;
   Controls.Add(tcDSP); tcDSP.TabPages.Add(tpDSPOptions); InitializeWdspNrTab();
   tpDSPOptions.Controls.AddRange(new Control[]{grpDSPLMSNR,grpDSPNB,grpDSPNB2,grpDSPLMSANF,grpDSPBufferSize,grpDSPWindow,
    btnRSTNR,btnRSTNB,btnRSTNB2,btnRSTANF,labelTS28,labelTS7,chkDSPRX2,chkDSPTXMeterPeak,udTNFWidth});
   for(int i=0;i<4;i++) {
    grpDSPLMSANF.Controls.Add(NewLabel(new string[]{"Taps:","Gain:","Delay:","Leak:"}[i],8,24+24*i,40));
    NumericUpDownTS n=new NumericUpDownTS(); n.Location=new Point(56,24+24*i); n.Size=new Size(48,20); n.Maximum=100000;
    n.Value=new decimal[]{68,25,60,1}[i];grpDSPLMSANF.Controls.Add(n);
   }
   for(int i=0;i<3;i++) {
    GroupBoxTS g=NewGroup(new string[]{"Phone","CW","Digital"}[i],8,18+76*i,104,72);
    g.Controls.Add(NewLabel("RX:",6,23,26));g.Controls.Add(NewCombo("rx"+i,34,20,64,new string[]{"4096"},0));
    if(i!=1) {g.Controls.Add(NewLabel("TX:",6,48,26));g.Controls.Add(NewCombo("tx"+i,34,45,64,new string[]{"4096"},0));}
    grpDSPBufferSize.Controls.Add(g);
   }
   grpDSPWindow.Controls.Add(NewCombo("window",8,23,104,new string[]{"Hamming"},0));
   chkDSPRX2.Checked=true; chkDSPTXMeterPeak.Checked=true; chkDSPTXMeterPeak.Text="Use Peak Readings for\nTX Meter DSP Values";
   udTNFWidth.Size=new Size(60,20); udTNFWidth.Value=100; labelTS7.Height=32;
   InitializeWdspNbOptions(); initializing=false; ApplyWdspNbSettings();
  }
  static void Check(Control parent) {
   foreach(Control c in parent.Controls) {
    if(c is GroupBox || c is TabPage) {
     foreach(Control a in c.Controls) foreach(Control b in c.Controls) {
      if(a.Visible && b.Visible && a is Label && b is NumericUpDown && a.Bounds.IntersectsWith(b.Bounds))
       throw new Exception("Label overlaps numeric: "+b.Name);
     }
    }
    Check(c);
   }
  }
  [STAThread] static void Main() {
   Application.EnableVisualStyles();
   using(Setup f=new Setup()) {
    f.ShowInTaskbar=false; f.Opacity=0; f.Show();
    f.CreateControl(); f.tcDSP.CreateControl(); f.tpWdspNRs.CreateControl();
    f.PerformLayout(); Check(f);
    if(f.grpDSPLMSNR.Visible || f.grpDSPNB.Visible || f.grpDSPNB2.Visible)
     throw new Exception("Old NR/NB groups must be hidden");
    f.wdspNb1Controls[0].Value=42.5M; f.wdspNb2Controls[2].Value=0.75M;
    f.comboWdspNb2Mode.SelectedIndex=4;
    if(DspBackend.Nb1Settings.Threshold!=42.5 || DspBackend.Nb2Settings.Lead!=0.00075 || DspBackend.Nb2Settings.Mode!=4)
     throw new Exception("Settings not published with correct units/mode");
    f.ResetWdspNb(f.wdspNb1Controls);f.ResetWdspNb(f.wdspNb2Controls);f.comboWdspNb2Mode.SelectedIndex=0;
    if(DspBackend.Nb1Settings.Threshold!=30 || DspBackend.Nb2Settings.Lead!=0.0001 || DspBackend.Nb2Settings.Mode!=0)
     throw new Exception("Defaults not restored");
    using(Bitmap b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)) {
     f.tcDSP.DrawToBitmap(b,new Rectangle(Point.Empty,f.tcDSP.Size)); b.Save("nb-layout-100.png");
    }
    f.Scale(new SizeF(1.5f,1.5f)); f.PerformLayout(); Check(f);
    using(Bitmap b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)) {
     f.tcDSP.DrawToBitmap(b,new Rectangle(Point.Empty,f.tcDSP.Size)); b.Save("nb-layout-150.png");
    }
    Console.WriteLine("PASS: no numeric/label overlaps at 100% and 150%; previews rendered.");
   }
  }
 }
}
