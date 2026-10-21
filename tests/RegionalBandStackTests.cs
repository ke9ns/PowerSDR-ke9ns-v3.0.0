using System;
using System.Data;
using System.IO;
using System.Reflection;
namespace PowerSDR
{
    internal static class RegionalBandStackTests
    {
        static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
        static int Main(string[] args)
        {
            try
            {
                var app = Assembly.LoadFrom(Path.GetFullPath(args[0]));
                var db = app.GetType("PowerSDR.DB", true);
                var flags = BindingFlags.Static | BindingFlags.NonPublic;
                var data = db.GetField("ds", BindingFlags.Static | BindingFlags.Public);
                var path = db.GetField("file_name", flags);
                var regionType = app.GetType("PowerSDR.FRSRegion", true);
                foreach (string region in new[] { "IARU2", "Australia" })
                {
                    var ds = new DataSet("RegionalTest");
                    data.SetValue(null, ds);
                    string file = Path.Combine(Path.GetFullPath(args[1]), region + "-" + Guid.NewGuid().ToString("N") + ".xml");
                    path.SetValue(null, file);
                    db.GetProperty("BandTextRefresh").SetValue(null, false, null);
                    db.GetProperty("BandStackRefresh").SetValue(null, true, null);
                    db.GetMethod("AddBandTextTable", flags).Invoke(null, null);
                    db.GetMethod("AddBandStackTable", flags).Invoke(null, null);
                    var original = ds.Tables["BandStack"];
                    var update = db.GetMethod("UpdateRegion");
                    object value = Enum.Parse(regionType, region);
                    update.Invoke(null, new[] { value });
                    Check(Object.ReferenceEquals(original, ds.Tables["BandStack"]), "Existing table replaced");
                    int count = original.Rows.Count;
                    Check(count > 0, "No regional defaults");
                    update.Invoke(null, new[] { value });
                    Check(original.Rows.Count == count, "Repeated refresh duplicated rows");
                    original.Rows.Add("QA-LOCKED", "USB", "F6", 7.123456);
                    db.GetProperty("BandStackRefresh").SetValue(null, false, null);
                    update.Invoke(null, new[] { value });
                    Check(original.Rows.Count == count + 1 && original.Select("BandName = 'QA-LOCKED'").Length == 1,
                        "Disabled refresh must preserve user entries");
                    var restored = new DataSet(); restored.ReadXml(file, XmlReadMode.ReadSchema);
                    Check(restored.Tables["BandStack"].Rows.Count == count + 1, "Saved profile mismatch");
                    ds.Tables.Remove(original);
                    db.GetProperty("BandStackRefresh").SetValue(null, true, null);
                    update.Invoke(null, new[] { value });
                    Check(ds.Tables["BandStack"].Rows.Count == count, "Missing table not initialized");
                    System.Console.WriteLine("PASS: " + region + " fresh/default table, repeat refresh, preserve user entries, XML round trip and missing table.");
                }
                return 0;
            }
            catch (Exception ex) { System.Console.WriteLine(ex); return 1; }
        }
    }
}
