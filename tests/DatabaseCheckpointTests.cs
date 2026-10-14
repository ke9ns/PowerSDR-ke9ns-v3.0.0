using System;
using System.Data;
using System.IO;
using System.Reflection;

namespace PowerSDR
{
    internal static class DatabaseCheckpointTests
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static int Main(string[] args)
        {
            try
            {
                Assembly app = Assembly.LoadFrom(Path.GetFullPath(args[0]));
                Check(app.GetName().Version.ToString() == "3.0.0.4", "Expected tested version 3.0.0.4");
                Type db = app.GetType("PowerSDR.DB", true);
                FieldInfo data = db.GetField("ds", BindingFlags.Static | BindingFlags.Public);
                FieldInfo path = db.GetField("file_name", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo write = db.GetMethod("WriteDatabase", BindingFlags.Static | BindingFlags.NonPublic);
                string output = Path.Combine(Path.GetFullPath(args[1]), "checkpoint-" + Guid.NewGuid().ToString("N") + ".xml");
                DataSet dataset = new DataSet("SyntheticShutdownTest");
                DataTable state = dataset.Tables.Add("State");
                state.Columns.Add("Name", typeof(string));
                state.Columns.Add("Value", typeof(string));
                state.Rows.Add("TestMarker", "3.0.0.3 - VOACAP on");
                data.SetValue(null, dataset);
                path.SetValue(null, output);
                write.Invoke(null, null);
                Check(Object.ReferenceEquals(data.GetValue(null), dataset), "Checkpoint must keep live dataset");
                DataSet restored = new DataSet();
                restored.ReadXml(output, XmlReadMode.ReadSchema);
                Check((string)restored.Tables["State"].Rows[0]["Value"] == "3.0.0.3 - VOACAP on", "XML checkpoint round trip");
                path.SetValue(null, Path.Combine(output, "missing-parent.xml"));
                bool failed = false;
                try { write.Invoke(null, null); }
                catch (TargetInvocationException ex) { failed = ex.InnerException is IOException; }
                Check(failed, "Write error must reach shutdown logger, not silently succeed");
                path.SetValue(null, output);
                state.Rows[0]["Value"] = "final save";
                db.GetMethod("Exit", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                Check(data.GetValue(null) == null, "Normal Exit still releases dataset");
                restored = new DataSet(); restored.ReadXml(output, XmlReadMode.ReadSchema);
                Check((string)restored.Tables["State"].Rows[0]["Value"] == "final save", "Normal final save preserved");
                System.Console.WriteLine("PASS: actual " + app.GetName().Version + " DB code: XML round trip, checkpoint retains ds, errors propagate, final Exit persists/releases. Synthetic file: " + output);
                return 0;
            }
            catch (Exception ex) { System.Console.WriteLine(ex); return 1; }
        }
    }
}
