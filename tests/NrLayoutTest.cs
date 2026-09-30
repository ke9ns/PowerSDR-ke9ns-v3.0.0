using System;
using System.Drawing;
using System.Windows.Forms;
namespace PowerSDR {
 public class LabelTS:Label {} public class GroupBoxTS:GroupBox {}
 public class ComboBoxTS:ComboBox {} public class ButtonTS:Button {}
 public class NumericUpDownTS:NumericUpDown {} public class CheckBoxTS:CheckBox {}
 public class TextBoxTS:TextBox {}
 public static class DspBackend {
  public static void SetRx1NrParameters(params object[] values) {}
  public static void LoadRnnrModel(string p) {}
 }
 public partial class Setup:Form {
  bool initializing=true;
  ToolTip toolTip1=new ToolTip(); TabControl tcDSP=new TabControl();
  public Setup() {
   Font=new Font("Microsoft Sans Serif",8.25f);
   ClientSize=new Size(600,345); tcDSP.Dock=DockStyle.Fill;
   Controls.Add(tcDSP); InitializeWdspNrTab();
  }
  static void Check(Control parent) {
   foreach(Control c in parent.Controls) {
    if(c is GroupBox || c is TabPage) {
     foreach(Control a in c.Controls) foreach(Control b in c.Controls) {
      if(a is Label && b is NumericUpDown && a.Bounds.IntersectsWith(b.Bounds))
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
    using(Bitmap b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)) {
     f.tcDSP.DrawToBitmap(b,new Rectangle(Point.Empty,f.tcDSP.Size)); b.Save("nr-layout-100.png");
    }
    f.Scale(new SizeF(1.5f,1.5f)); f.PerformLayout(); Check(f);
    using(Bitmap b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)) {
     f.tcDSP.DrawToBitmap(b,new Rectangle(Point.Empty,f.tcDSP.Size)); b.Save("nr-layout-150.png");
    }
    Console.WriteLine("PASS: no numeric/label overlaps at 100% and 150%; previews rendered.");
   }
  }
 }
}
