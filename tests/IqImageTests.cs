using System;
using System.Threading;
namespace PowerSDR
{
    internal unsafe class IqImageTests
    {
        const int N = 2048;
        static long phase;
        static double Measure(WdspReceiver receiver, DSPRX rx, double frequency, double qGain)
        {
            float[] il = new float[N], ir = new float[N], ol = new float[N], orr = new float[N];
            double power = 0; int samples = 0;
            int blocks = (int)Math.Ceiling(1.6 * Audio.SampleRate1 / N);
            for (int b = 0; b < blocks; b++)
            {
                for (int i = 0; i < N; i++, phase++)
                {
                    double a = 2 * Math.PI * frequency * phase / Audio.SampleRate1;
                    il[i] = (float)(0.1 * Math.Sin(a));
                    ir[i] = (float)(0.1 * Math.Cos(a) * qGain);
                }
                float beforeL = il[10], beforeR = ir[10];
                fixed (float* l = il, r = ir, o = ol, p = orr)
                {
                    if (!receiver.Process(rx, l, r, N)) throw new Exception("WDSP fallback");
                    receiver.Mix(rx, o, p, N, false, false);
                }
                if (il[10] != beforeL || ir[10] != beforeR)
                    throw new Exception("Shared input modified");
                if (b > blocks / 2)
                    for (int i = 0; i < N; i++) { power += ol[i] * ol[i]; samples++; }
                Thread.Sleep((int)Math.Ceiling(1000.0 * N / Audio.SampleRate1));
            }
            return Math.Sqrt(power / samples);
        }
        static void Main()
        {
            foreach (int channel in new[] { 0, 1, 2 })
            {
                var receiver = new WdspReceiver(channel);
                var rx = new DSPRX { Active = true, DSPMode = DSPMode.DIGU,
                    RXFilterLow = 0, RXFilterHigh = 3000, RXOsc = -10000 };
                foreach (int rate in new[] { 48000, 96000, 192000 })
                {
                    Audio.SampleRate1 = rate;
                    rx.IQSnapshotAvailable = true;
                    Array.Clear(rx.IQCorrection, 0, 5);
                    double pass = Measure(receiver, rx, 11000, 1);
                    double raw = Measure(receiver, rx, -11000, 1.02);
                    rx.IQCorrection[0] = 1;
                    rx.IQCorrection[1] = (float)(0.02 / 2.02);
                    double corrected = Measure(receiver, rx, -11000, 1.02);
                    if (pass < 0.01 || raw < pass * 0.005 || corrected > raw * 1e-4)
                        throw new Exception("Image rejection failure");
                    // A busy legacy DSP must reuse the last complete snapshot.
                    rx.IQSnapshotAvailable = false;
                    double cached = Measure(receiver, rx, -11000, 1.02);
                    if (cached > raw * 1e-4) throw new Exception("Snapshot cache failed");
                    rx.IQSnapshotAvailable = true;
                    // Same correction via adaptive w[1], not static w[0].
                    rx.IQCorrection[1] = 0;
                    rx.IQCorrection[3] = (float)(0.02 / 2.02);
                    double adaptive = Measure(receiver, rx, -11000, 1.02);
                    if (adaptive > raw * 1e-4) throw new Exception("Adaptive stage ignored");
                    rx.IQCorrection[0] = 0;
                    double off = Measure(receiver, rx, -11000, 1.02);
                    if (off < raw * 0.9) throw new Exception("IQ disable ignored");
                    System.Console.WriteLine("PASS channel={0} rate={1} image={2:F1} dBc corrected={3:F1} dBc; cache/adaptive/disable/input isolation",
                        channel, rate, 20 * Math.Log10(raw/pass), 20 * Math.Log10(corrected/pass));
                }
            }
            Environment.Exit(0);
        }
    }
}
