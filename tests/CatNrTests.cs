using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
class CatNrTests
{
    static int Main(string[] args)
    {
        try
        {
            var app = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            var ct = app.GetType("PowerSDR.Console", true);
            // No constructor, UI handle, driver or radio is initialized.
            var console = FormatterServices.GetUninitializedObject(ct);
            GC.SuppressFinalize(console);
            var pt = app.GetType("PowerSDR.CATParser", true);
            var parser = Activator.CreateInstance(pt, new[] { console });
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var commands = pt.GetField("cmdlist", flags).GetValue(parser);
            int[] modes = { 0, 0 };
            int writes = 0;
            Func<bool, int?, int> selection = delegate(bool rx2, int? n) {
                int i = rx2 ? 1 : 0;
                if (n.HasValue) { modes[i] = n.Value; writes++; }
                return modes[i];
            };
            commands.GetType().GetField("NrSelection", flags).SetValue(commands, selection);
            var get = pt.GetMethod("Get", new[] { typeof(string) });
            Func<string,string> send = s => (string)get.Invoke(parser, new object[] { s });
            foreach (string cmd in new[] { "ZZNE", "ZZNF" })
                for (int n = 0; n <= 6; n++) {
                    if (send(cmd + n + ";") != "" || send(cmd + ";") != cmd + n + ";")
                        throw new Exception("CAT set/read mismatch " + cmd + n);
                }
            send("ZZNE2;");
            if (send("ZZNF;") != "ZZNF6;") throw new Exception("RX2 not independent");
            int before = writes;
            foreach (string bad in new[] { "ZZNE7;", "ZZNF9;", "ZZNE-1;", "ZZNF06;", "ZZNEA;" })
                if (send(bad) != "?;") throw new Exception("Invalid command accepted: " + bad);
            if (writes != before || modes[0] != 2 || modes[1] != 6) throw new Exception("Invalid write changed state");
            if (send("ZZNR;") != "ZZNR0;") throw new Exception("Legacy NR status changed");
            commands.GetType().GetField("NrSelection", flags).SetValue(commands,
                new Func<bool,int?,int>((r,n) => { throw new InvalidOperationException("test"); }));
            if (send("ZZNE1;") != "?;") throw new Exception("Backend failure not reported");
            System.Console.WriteLine("PASS: real CAT parser/XML, OFF and NR1-6 on both RX, query framing, independent RX, invalid inputs and backend failure.");
            return 0;
        }
        catch(Exception e) { System.Console.WriteLine(e); return 1; }
    }
}
