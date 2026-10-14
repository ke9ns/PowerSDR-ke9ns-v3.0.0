using System;
using System.Threading;
namespace PowerSDR
{
    internal static unsafe class CwAudioTapTests
    {
        static int Main()
        {
            try
            {
                var dsp = new DSP(); dsp.Main.Active = true;
                dsp.Main.DSPMode = DSPMode.CWU; dsp.Main.RXOutputGain = 0;
                DspBackend.Register(dsp);
                const int n = 2048;
                var l = new float[n]; var r = new float[n]; var ol = new float[n]; var oright = new float[n];
                int calls = 0; double energy = 0;
                DspBackend.CwAudioSink = delegate(IntPtr a, IntPtr b, int count, int rate)
                {
                    calls++;
                    for (int i = 0; i < count; i++) energy += ((float*)a)[i] * ((float*)a)[i];
                };
                fixed (float* lp = l, rp = r, op = ol, oq = oright)
                {
                    for (int block = 0; block < 40; block++)
                    {
                        for (int i = 0; i < n; i++) { double angle = 2 * Math.PI * 600 * (block * n + i) / 48000; l[i] = (float)(0.001 * Math.Sin(angle)); r[i] = (float)(0.001 * Math.Cos(angle)); }
                        DspBackend.ProcessStereo(lp, rp, op, oq, n, true); Thread.Sleep(43);
                    }
                    if (calls == 0 || energy <= 0) throw new Exception("No RX1 capture before muted AF gain");
                    int before = calls;
                    DspBackend.ProcessStereo(lp, rp, op, oq, n, false);
                    dsp.Main.DSPMode = DSPMode.USB;
                    DspBackend.ProcessStereo(lp, rp, op, oq, n, true);
                    if (calls != before) throw new Exception("Capture occurred in TX or USB");
                    dsp.Main.DSPMode = DSPMode.CWU;
                    DspBackend.CwAudioSink = delegate { throw new Exception("Synthetic decoder failure"); };
                    DspBackend.ProcessStereo(lp, rp, op, oq, n, true);
                    DspBackend.CwAudioSink = null;
                }
                Console.WriteLine("PASS: RX1 CW pre-AF capture, TX/USB excluded, decoder exception isolated.");
                return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }
    }
}
