using System;
using System.Threading;
namespace PowerSDR
{
    internal unsafe class IqImageTests
    {
        const int N = 2048;
        static long phase;
        static double Measure(WdspReceiver receiver, DSPRX rx, double frequency, double qGain,
            double phaseError = 0)
        {
            float[] il = new float[N], ir = new float[N], ol = new float[N], orr = new float[N];
            double power = 0; int samples = 0;
            int blocks = (int)Math.Ceiling(1.6 * Audio.SampleRate1 / N);
            for (int b = 0; b < blocks; b++)
            {
                for (int i = 0; i < N; i++, phase++)
                {
                    double a = 2 * Math.PI * frequency * phase / Audio.SampleRate1;
                    il[i] = (float)(0.1 * (Math.Sin(a) + phaseError * Math.Cos(a)));
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
                    // DttSP/sdr.c process_samples maps real=RIGHT, imag=LEFT.
                    // Input z = g*cos(a) + j*(sin(a)+p*cos(a)).
                    // Derive cancellation analytically from its image component:
                    // w = -(g-1+j*p)/(g+1-j*p), NOT from bridge implementation.
                    const double g = 1.02, p = 0.015;
                    double den = (g+1)*(g+1) + p*p;
                    float wr = (float)(-(g*g-1-p*p)/den);
                    float wi = (float)(-2*g*p/den);
                    double raw = Measure(receiver, rx, -11000, g, p);
                    rx.IQCorrection[0] = 1;
                    rx.IQCorrection[1] = wr;
                    rx.IQCorrection[2] = wi;
                    double corrected = Measure(receiver, rx, -11000, g, p);
                    if (pass < 0.01 || raw < pass * 0.005 || corrected > raw * 1e-4)
                        throw new Exception("Image rejection failure");
                    // A busy legacy DSP must reuse the last complete snapshot.
                    rx.IQSnapshotAvailable = false;
                    double cached = Measure(receiver, rx, -11000, g, p);
                    if (cached > raw * 1e-4) throw new Exception("Snapshot cache failed");
                    rx.IQSnapshotAvailable = true;
                    // Same correction via adaptive w[1], not static w[0].
                    rx.IQCorrection[1] = 0;
                    rx.IQCorrection[2] = 0;
                    rx.IQCorrection[3] = wr;
                    rx.IQCorrection[4] = wi;
                    double adaptive = Measure(receiver, rx, -11000, g, p);
                    if (adaptive > raw * 1e-4) throw new Exception("Adaptive stage ignored");
                    rx.IQCorrection[0] = 0;
                    double off = Measure(receiver, rx, -11000, g, p);
                    if (off < raw * 0.9) throw new Exception("IQ disable ignored");
                    System.Console.WriteLine("PASS channel={0} rate={1} image={2:F1} dBc corrected={3:F1} dBc; cache/adaptive/disable/input isolation",
                        channel, rate, 20 * Math.Log10(raw/pass), 20 * Math.Log10(corrected/pass));
                }
            }
            Environment.Exit(0);
        }
    }
}
