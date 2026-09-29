using System;

namespace PowerSDR
{
    // Immutable configuration published by the UI. Times are seconds.
    internal sealed class WdspBlankerSettings
    {
        internal readonly double Threshold, Slew, Lead, Hang, Average;
        internal readonly int Mode;
        internal WdspBlankerSettings(double threshold, double slew, double lead,
            double hang, double average, int mode)
        {
            Threshold = Math.Max(1, Math.Min(10000, threshold));
            Slew = Math.Max(0, Math.Min(0.002, slew));
            Lead = Math.Max(0, Math.Min(0.002, lead));
            Hang = Math.Max(0, Math.Min(0.002, hang));
            Average = Math.Max(0.001, Math.Min(1, average));
            Mode = Math.Max(0, Math.Min(4, mode));
        }
        internal static WdspBlankerSettings Default()
        { return new WdspBlankerSettings(30, 0.0001, 0.0001, 0.0001, 0.05, 0); }
    }

    // Thetis ChannelMaster's ANB/NOB algorithms through WDSP's double-IQ API.
    // Avoid the native legacy float API, whose temporary buffer is only 2048.
    // All native creation, processing and destruction belong to the audio thread.
    internal sealed unsafe class WdspImpulseBlankers : IDisposable
    {
        private readonly int id;
        private bool anbCreated, nobCreated, wasNb1, wasNb2;
        private int size, rate;
        private WdspBlankerSettings applied1, applied2;
        private double[] iq;
        internal float[] Left, Right;
        internal WdspImpulseBlankers(int channel) { id = 16 + channel; }

        internal bool Process(float* left, float* right, int count, int sampleRate,
            bool nb1, bool nb2, WdspBlankerSettings settings1, WdspBlankerSettings settings2)
        {
            if (!nb1 && !nb2) { wasNb1 = wasNb2 = false; return false; }
            if (count <= 0 || sampleRate < 8000 || sampleRate > 1536000)
                throw new ArgumentOutOfRangeException("sampleRate");
            if (count != size || sampleRate != rate)
            {
                Dispose();
                size = count; rate = sampleRate;
                iq = new double[count * 2]; Left = new float[count]; Right = new float[count];
            }
            if (nb1 && (!anbCreated || applied1 != settings1))
            {
                if (anbCreated) { WdspNative.destroy_anbEXT(id); anbCreated = false; }
                WdspNative.create_anbEXT(id, 1, size, rate, settings1.Slew,
                    settings1.Hang, settings1.Lead, settings1.Average, settings1.Threshold);
                anbCreated = true; applied1 = settings1; wasNb1 = false;
            }
            if (nb2 && (!nobCreated || applied2 != settings2))
            {
                if (nobCreated) { WdspNative.destroy_nobEXT(id); nobCreated = false; }
                WdspNative.create_nobEXT(id, 1, settings2.Mode, size, rate, settings2.Slew,
                    settings2.Hang, settings2.Lead, settings2.Average, settings2.Threshold);
                nobCreated = true; applied2 = settings2; wasNb2 = false;
            }
            if (nb1 && !wasNb1) WdspNative.flush_anbEXT(id);
            if (nb2 && !wasNb2) WdspNative.flush_nobEXT(id);
            for (int i = 0; i < count; i++) { iq[2*i] = left[i]; iq[2*i+1] = right[i]; }
            fixed (double* p = iq)
            {
                if (nb1) WdspNative.xanbEXT(id, p, p);
                if (nb2) WdspNative.xnobEXT(id, p, p);
            }
            for (int i = 0; i < count; i++) { Left[i] = (float)iq[2*i]; Right[i] = (float)iq[2*i+1]; }
            wasNb1 = nb1; wasNb2 = nb2;
            return true;
        }

        public void Dispose()
        {
            if (anbCreated) { WdspNative.destroy_anbEXT(id); anbCreated = false; }
            if (nobCreated) { WdspNative.destroy_nobEXT(id); nobCreated = false; }
            wasNb1 = wasNb2 = false;
            applied1 = applied2 = null;
            size = rate = 0;
            iq = null; Left = Right = null;
        }
    }
}
