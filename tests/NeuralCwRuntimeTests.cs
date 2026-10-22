using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

internal static class NeuralCwRuntimeTests
{
    static int Main(string[] args)
    {
        try
        {
            if (IntPtr.Size != 4) throw new Exception("Must run in x86");
            int rate = args.Length > 0 ? Int32.Parse(args[0]) : 48000;
            bool silence = args.Length > 1 && args[1] == "silence";
            var app = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PowerSDR.exe"));
            var type = app.GetType("PowerSDR.NeuralCwDecoder", true);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var instance = type.GetConstructor(flags, null, new[] { typeof(int), typeof(double) }, null).Invoke(new object[] { rate, 4.0 });
            string decoded = ""; object guard = new object();
            Action<string> collect = delegate(string text) { lock (guard) decoded += text; };
            type.GetEvent("TextReceived", flags).GetAddMethod(true).Invoke(instance, new object[] { collect });
            var capture = (Action<IntPtr, IntPtr, int, int>)Delegate.CreateDelegate(typeof(Action<IntPtr, IntPtr, int, int>), instance, type.GetMethod("Capture", flags));
            type.GetMethod("Start", flags).Invoke(instance, null);
            var pattern = new List<bool>();
            int dot = rate * 60 / 1000;
            foreach (string letter in new[] { "-.-.", "--.-", "-.-.", "--.-", "-.-.", "--.-" })
            {
                foreach (char mark in letter)
                {
                    for (int i = 0; i < (mark == '-' ? 3 : 1) * dot; i++) pattern.Add(true);
                    for (int i = 0; i < dot; i++) pattern.Add(false);
                }
                for (int i = 0; i < dot * 3; i++) pattern.Add(false);
            }
            for (int i = 0; i < rate * 3; i++) pattern.Add(false);
            float[] left = new float[rate / 50], right = new float[rate / 50];
            var lh = GCHandle.Alloc(left, GCHandleType.Pinned); var rh = GCHandle.Alloc(right, GCHandleType.Pinned);
            try
            {
                for (int offset = 0; offset < pattern.Count; offset += left.Length)
                {
                    int count = Math.Min(left.Length, pattern.Count - offset);
                    for (int i = 0; i < count; i++) right[i] = !silence && pattern[offset + i] ? (float)(0.1 * Math.Sin(2 * Math.PI * 600 * (offset + i) / rate)) : 0;
                    capture(lh.AddrOfPinnedObject(), rh.AddrOfPinnedObject(), count, rate);
                    Thread.Sleep(20);
                }
                Thread.Sleep(1000);
                string error = (string)type.GetProperty("Error", flags).GetValue(instance, null);
                if (error != null) throw new Exception(error);
                ((IDisposable)instance).Dispose();
                // A callback captured just before UI disable must be safe after disposal.
                capture(lh.AddrOfPinnedObject(), rh.AddrOfPinnedObject(), left.Length, rate);
            }
            finally { lh.Free(); rh.Free(); ((IDisposable)instance).Dispose(); }
            System.Console.WriteLine("x86 " + rate + "Hz " + (silence ? "silence" : "CQ") + ": [" + decoded + "]");
            if (silence ? decoded.Trim().Length != 0 : !(decoded.Contains("C") && decoded.Contains("Q"))) return 1;
            System.Console.WriteLine("PASS: actual application decoder, real x86 ONNX inference and stale callback after disposal.");
            return 0;
        }
        catch (Exception ex) { System.Console.Error.WriteLine(ex); return 1; }
    }
}
