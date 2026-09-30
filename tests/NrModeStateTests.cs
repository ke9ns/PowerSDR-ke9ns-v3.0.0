using System;
using System.Collections;
using System.Data;
using System.IO;
namespace PowerSDR
{
    internal static class NrModeStateTests
    {
        static void Check(bool ok) { if (!ok) throw new Exception("NR state regression"); }
        static int Main()
        {
            for (int mode = 0; mode <= 4; mode++)
            {
                // Round-trip the same Key/Value XML representation used by State.
                var data = new DataSet();
                var table = data.Tables.Add("State");
                table.Columns.Add("Key"); table.Columns.Add("Value");
                string[] parts = NrModeState.Serialize(mode).Split('/');
                table.Rows.Add(parts[0], parts[1]);
                table.Rows.Add("chkNR", (mode != 0).ToString());
                var xml = new StringWriter(); data.WriteXml(xml, XmlWriteMode.WriteSchema);
                var restored = new DataSet(); restored.ReadXml(new StringReader(xml.ToString()));
                var entries = new ArrayList();
                foreach (DataRow row in restored.Tables["State"].Rows)
                    entries.Add(row[0] + "/" + row[1]);
                entries.Sort();
                Check(NrModeState.Restore(entries, true) == mode);
                Check(NrModeState.Restore(entries, false) == mode);
            }
            foreach (string value in new[] { "bad", "", "-1", "5", "999999999999" })
            {
                var invalid = new ArrayList { "WdspRX1NRMode/" + value };
                Check(NrModeState.Restore(invalid, true) == 1);
                Check(NrModeState.Restore(invalid, false) == 0);
            }
            Check(NrModeState.Restore(new ArrayList(), true) == 1);
            Check(NrModeState.Restore(new ArrayList(), false) == 0);
            for (int rx1 = 0; rx1 <= 4; rx1++)
                for (int rx2 = 0; rx2 <= 4; rx2++)
                {
                    var entries = new ArrayList { NrModeState.Serialize(rx1), NrModeState.Serialize(rx2, true) };
                    Check(NrModeState.Restore(entries, false) == rx1);
                    Check(NrModeState.Restore(entries, false, true) == rx2);
                }
            Check(NrModeState.Restore(new ArrayList { "WdspRX1NRMode/4" }, true, true) == 1);
            Check(NrModeState.Restore(new ArrayList { "WdspRX2NRMode/5" }, false, true) == 0);
            Console.WriteLine("PASS: OFF/NR1/NR2/NR3/NR4 XML round-trip, legacy profiles and invalid values.");
            return 0;
        }
    }
}
