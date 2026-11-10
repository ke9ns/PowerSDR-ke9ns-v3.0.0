using System;
using System.Reflection;
using System.Windows.Forms;
namespace PowerSDR {
 public class NumericUpDownTS : NumericUpDown {}
 public class GroupBoxTS : GroupBox {}
 public class ButtonTS : Button {}
 public class LabelTS : Label {}
 public partial class Setup : Form {
  private bool initializing = true;
  private TabControl tcDSP = new TabControl(), tcSetup = new TabControl();
  private TabPage tpWdspNRs = new TabPage();
  private ToolTip toolTip1 = new ToolTip();
  public Setup() { tcDSP.TabPages.Add(tpWdspNRs); InitializeNnrTab(); initializing=false; }
  public void ApplyAll() { ApplyNnrSettings(); }
  public void Edit(int receiver) { nnrControls[receiver,0].Value += 1; }
  public void Press(int receiver, string label) {
   foreach(Control c in tcDSP.TabPages[1].Controls)
    if(c is GroupBoxTS && c.Text == (receiver == 0 ? "RX1 / RX-S" : "RX2"))
     foreach(Control b in c.Controls)
      if(b is ButtonTS && b.Text == label) {
       typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,new object[]{EventArgs.Empty}); return;
      }
   throw new Exception("Button missing");
  }
 }
 internal class NnrReceiverIsolationTests {
  static WdspReceiver Rx(string name) { return (WdspReceiver)typeof(DspBackend).GetField(name,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null); }
  static int Version(string name) { return (int)typeof(WdspReceiver).GetField("nrConfigurationVersion",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(Rx(name)); }
  static int[] Versions() { return new int[]{Version("main"),Version("sub"),Version("rx2")}; }
  static void Check(Action action, int a, int b, int c, string label) {
   int[] before=Versions(); action(); int[] after=Versions(), delta={a,b,c};
   for(int i=0;i<3;i++) if(after[i]-before[i]!=delta[i]) throw new Exception(label+": receiver "+i+" changed "+(after[i]-before[i])+" times, expected "+delta[i]);
   System.Console.WriteLine("PASS "+label);
  }
  [STAThread] static int Main() {
   try { using(var setup=new Setup()) {
    Check(delegate {setup.Press(1,"Voice smooth");},0,0,1,"RX2 preset isolation");
    Check(delegate {setup.Press(0,"Voice smooth");},1,1,0,"RX1/RX-S preset isolation");
    Check(delegate {setup.Press(1,"Voice smooth");},0,0,0,"Repeated preset");
    Check(delegate {setup.Edit(1);},0,0,1,"RX2 numeric edit");
    Check(delegate {setup.Edit(0);},1,1,0,"RX1 numeric edit");
    Check(delegate {setup.Press(1,"Default");},0,0,1,"RX2 default");
    Check(delegate {setup.Press(0,"Default");},1,1,0,"RX1 default");
    Check(delegate {setup.ApplyAll();},0,0,0,"Unchanged initialization");
    double[] values={-18,6,.75,12,3,20,120};
    Rx("main").SetNnrParameters(values); values[0]=-40;
    double[] stored=(double[])typeof(WdspReceiver).GetField("nnrParameters",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(Rx("main"));
    if(stored[0]!=-18) throw new Exception("Snapshot not cloned");
    System.Console.WriteLine("PASS immutable snapshot");
   } return 0; } catch(Exception ex) { System.Console.Error.WriteLine(ex); return 1; }
  }
 }
}