using System;
using System.IO;

namespace PowerSDR
{
    internal static unsafe class DspBackend
    {
        private static readonly WdspReceiver main = new WdspReceiver(0);
        private static readonly WdspReceiver sub = new WdspReceiver(1);
        private static readonly WdspReceiver rx2 = new WdspReceiver(2);
        private static readonly WdspRxTransition mainTransition = new WdspRxTransition();
        private static readonly WdspRxTransition rx2Transition = new WdspRxTransition();
        private static bool rx2TransitionMuted;

        // Publish commands only; all sample counters belong to the audio thread.
        // Audio.MOX can become false before HdwMOXChanged(false) has completed.
        internal static void BeginRadioTransition(bool mutePhysicalRx2)
        {
            mainTransition.Block();
            if (mutePhysicalRx2)
            {
                rx2TransitionMuted = true;
                rx2Transition.Block();
            }
        }
        internal static void CompleteReceiveTransition(int settleMilliseconds)
        {
            mainTransition.Release(settleMilliseconds);
            if (rx2TransitionMuted) rx2Transition.Release(settleMilliseconds);
            rx2TransitionMuted = false;
        }
        private static readonly bool requested = IsRequested();
        private static DSP receivers;
        private static long completedAudioBlocks;
        internal static long CompletedAudioBlocks
        { get { return System.Threading.Interlocked.Read(ref completedAudioBlocks); } }
        internal static volatile WdspBlankerSettings Nb1Settings = WdspBlankerSettings.Default();
        internal static volatile WdspBlankerSettings Nb2Settings = WdspBlankerSettings.Default();
        private sealed class DiversitySettings
        {
            internal readonly bool Enabled;
            internal readonly float Real, Imag, Gain;
            internal DiversitySettings(bool enabled, float real, float imag, float gain)
            { Enabled = enabled; Real = real; Imag = imag; Gain = gain; }
        }
        private static readonly object diversityLock = new object();
        private static volatile DiversitySettings diversity = new DiversitySettings(false, 0, 0, 1);
        private static float[] diversityLeft, diversityRight;
        internal static void SetDiversity(bool value)
        {
            lock (diversityLock)
            { DiversitySettings d = diversity; diversity = new DiversitySettings(value, d.Real, d.Imag, d.Gain); }
        }
        internal static void SetDiversityScalar(float real, float imag)
        {
            lock (diversityLock)
            { DiversitySettings d = diversity; diversity = new DiversitySettings(d.Enabled, real, imag, d.Gain); }
        }
        internal static void SetDiversityGain(float value)
        {
            lock (diversityLock)
            { DiversitySettings d = diversity; diversity = new DiversitySettings(d.Enabled, d.Real, d.Imag, value); }
        }
        internal static void Register(DSP value) { receivers = value; }
        private static readonly object preparationLock = new object();
        private static bool wisdomLoaded;
        internal static void PrepareForAudio(int count, int rate, string profileDirectory)
        {
            DSP dsp = receivers;
            if (!requested || dsp == null || count <= 0 || rate <= 0) return;
            lock (preparationLock)
            {
                mainTransition.Release(0);
                rx2Transition.Release(0);
                rx2TransitionMuted = false;
                string cache = String.IsNullOrEmpty(profileDirectory) ? null :
                    Path.Combine(profileDirectory, "wdsp-fftw-x86.wisdom");
                if (!wisdomLoaded)
                {
                    wisdomLoaded = true;
                    try
                    {
                        if (cache != null && File.Exists(cache))
                            WdspNative.fftw_import_wisdom_from_string(File.ReadAllText(cache));
                    }
                    catch (Exception ex)
                    { System.Diagnostics.Debug.WriteLine("WDSP cache ignored: " + ex.Message); }
                }
                main.Prepare(dsp.GetDSPRX(0, 0), count, rate);
                if (dsp.GetDSPRX(0, 1).Active) sub.Prepare(dsp.GetDSPRX(0, 1), count, rate);
                if (dsp.GetDSPRX(1, 0).Active) rx2.Prepare(dsp.GetDSPRX(1, 0), count, rate);
                if (cache == null) return;
                try
                {
                    string text = WdspNative.ExportWisdom();
                    Directory.CreateDirectory(profileDirectory);
                    string temporary = cache + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        File.WriteAllText(temporary, text);
                        if (File.Exists(cache)) File.Replace(temporary, cache, null);
                        else File.Move(temporary, cache);
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
                catch (Exception ex)
                { System.Diagnostics.Debug.WriteLine("WDSP cache not saved: " + ex.Message); }
            }
        }
        internal static bool ExperimentalWdspRequested { get { return requested; } }

        internal static void SetRx1Mode(DSPMode value) { main.SetRx1Mode(value); }
        internal static void SetRx1Filter(int low, int high) { main.SetRx1Filter(low, high); }
        internal static void SetRx1Agc(AGCMode value) { main.SetRx1Agc(value); }
        internal static void SetRx1NoiseReductionMode(int value)
        {
            main.SetRx1NoiseReductionMode(value); sub.SetRx1NoiseReductionMode(value);
        }
        internal static void SetRx1Anf(bool value) { main.SetRx1Anf(value); sub.SetRx1Anf(value); }
        internal static void SetRx2Anf(bool value) { rx2.SetRx1Anf(value); }
        internal static void SetRx2NoiseReductionMode(int value) { rx2.SetRx1NoiseReductionMode(value); }
        internal static bool LoadRnnrModel(string path) { return main.LoadRnnrModel(path); }

        internal static void SetRx1NrParameters(int position,
            int taps, int delay, int gain, int leak, int gainMethod, int npe, bool ae,
            int t1, int t2, bool post, int level, int factor, int rate, int taper,
            bool fixedGain, int reduction, int smoothing, int whitening, int rescale,
            int threshold, int algorithm)
        {
            main.SetRx1NrParameters(position, taps, delay, gain, leak, gainMethod, npe, ae,
                t1, t2, post, level, factor, rate, taper, fixedGain, reduction, smoothing,
                whitening, rescale, threshold, algorithm);
            sub.SetRx1NrParameters(position, taps, delay, gain, leak, gainMethod, npe, ae,
                t1, t2, post, level, factor, rate, taper, fixedGain, reduction, smoothing,
                whitening, rescale, threshold, algorithm);
            rx2.SetRx1NrParameters(position, taps, delay, gain, leak, gainMethod, npe, ae,
                t1, t2, post, level, factor, rate, taper, fixedGain, reduction, smoothing,
                whitening, rescale, threshold, algorithm);
        }

        public static void ProcessStereo(void* il, void* ir, void* ol, void* oright,
            int count, bool receiving)
        {
            DttSP.ExchangeSamples(il, ir, ol, oright, count);
            if (receiving) ProcessPair((float*)il, (float*)ir, (float*)ol, (float*)oright, count);
            System.Threading.Interlocked.Increment(ref completedAudioBlocks);
        }

        public static void ProcessMultiChannel(void* input, void* output, int count, bool receiving)
        {
            ProcessMultiChannelCore(input, output, count, receiving);
            System.Threading.Interlocked.Increment(ref completedAudioBlocks);
        }

        private static void ProcessMultiChannelCore(void* input, void* output, int count, bool receiving)
        {
            DttSP.ExchangeSamples2(input, output, count);
            if (input == null || output == null) return;
            float** ins = (float**)input;
            float** outs = (float**)output;
            // RX2 has its own IQ and audio pair. Keep it on WDSP during TX
            // only when the user's RX2 monitoring option permits it.
            if (receiving || !Audio.RX2AutoMuteTX)
                ProcessPhysicalRx2(ins[4], ins[5], outs[4], outs[5], count);
            if (!receiving) return;
            DiversitySettings d = diversity;
            if (requested && d.Enabled && count > 0 && ins[0] != null && ins[1] != null &&
                ins[4] != null && ins[5] != null)
            {
                if (diversityLeft == null || diversityLeft.Length != count)
                { diversityLeft = new float[count]; diversityRight = new float[count]; }
                // Match Audio_Callback2's complex scalar in its buffer ordering,
                // preserving the legacy buffer ordering used by this bridge.
                for (int i = 0; i < count; i++)
                {
                    diversityLeft[i] = (ins[0][i] + ins[4][i] * d.Real - ins[5][i] * d.Imag) * d.Gain;
                    diversityRight[i] = (ins[1][i] + ins[4][i] * d.Imag + ins[5][i] * d.Real) * d.Gain;
                }
                fixed (float* l = diversityLeft, r = diversityRight)
                    ProcessPair(l, r, outs[0], outs[1], count);
            }
            else ProcessPair(ins[0], ins[1], outs[0], outs[1], count);
            // Never overwrite pair 2/3: that is the transmitter stream.
        }

        private static void ProcessPhysicalRx2(float* il, float* ir, float* ol, float* oright, int count)
        {
            DSP dsp = receivers;
            if (!requested || dsp == null || count <= 0 || il == null || ir == null ||
                ol == null || oright == null) return;
            DSPRX receiver = dsp.GetDSPRX(1, 0);
            if (!rx2Transition.CanProcess(count, Audio.SampleRate1))
            { ClearPair(ol, oright, count); return; }
            // Do not create a native channel on radios without an active RX2.
            // Processing uses scratch buffers, leaving DttSP intact on failure.
            if (!receiver.Active || !rx2.Process(receiver, il, ir, count)) return;
            rx2.Mix(receiver, ol, oright, count, false, false);
            rx2Transition.Apply(ol, oright, count);
        }

        private static void ProcessPair(float* il, float* ir, float* ol, float* oright, int count)
        {
            DSP dsp = receivers;
            if (!requested || dsp == null || count <= 0 || il == null || ir == null ||
                ol == null || oright == null) return;
            DSPRX m = dsp.GetDSPRX(0, 0), s = dsp.GetDSPRX(0, 1);
            if (!mainTransition.CanProcess(count, Audio.SampleRate1))
            { ClearPair(ol, oright, count); return; }
            bool ma = m.Active, sa = s.Active;
            // Both consume original IQ before either overwrites the output.
            if (ma && !main.Process(m, il, ir, count)) return;
            if (sa && !sub.Process(s, il, ir, count)) return;
            if (ma) main.Mix(m, ol, oright, count, false, sa);
            if (sa) sub.Mix(s, ol, oright, count, ma, ma);
            if (!ma && !sa)
                for (int i = 0; i < count; i++) { ol[i] = 0; oright[i] = 0; }
            mainTransition.Apply(ol, oright, count);
        }

        private static void ClearPair(float* left, float* right, int count)
        { for (int i = 0; i < count; i++) { left[i] = 0; right[i] = 0; } }

        private static bool IsRequested()
        {
            string value = Environment.GetEnvironmentVariable("POWERSDR_WDSP_RX1");
            if (value == "0" || String.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                return false;
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "DISABLE_WDSP_RX1.txt"))) return false;

            // WDSP is now the normal RX1/RX-S backend. Earlier test builds
            // required ENABLE_WDSP_RX1.txt; omitting that marker silently left
            // NR2/NR3/NR4 as UI-only selections while NR1 still used DttSP.
            // WdspReceiver validates the DLL/version and falls back atomically
            // to DttSP if the native channel cannot be opened.
            return true;
        }
    }

    // Do not feed relay switching impulses or blanked hardware samples into
    // adaptive NR/AGC. Preserve their learned state while RX is unavailable.
    internal sealed unsafe class WdspRxTransition
    {
        private sealed class Request
        {
            internal readonly bool Ready;
            internal readonly int Settle;
            internal Request(bool ready, int settle) { Ready = ready; Settle = settle; }
        }
        private volatile Request request = new Request(true, 0);
        private Request applied;
        private int hold, ramp, rampSize;
        internal void Block() { request = new Request(false, 0); }
        internal void Release(int milliseconds) { request = new Request(true, Math.Max(0, milliseconds)); }
        internal bool CanProcess(int count, int rate)
        {
            Request current = request;
            if (!Object.ReferenceEquals(current, applied))
            {
                applied = current;
                hold = (int)((long)rate * current.Settle / 1000);
                ramp = 0;
                rampSize = Math.Max(1, rate / 200); // Five-millisecond raised-cosine ramp.
            }
            if (!current.Ready) return false;
            if (hold > 0) { hold = Math.Max(0, hold - count); return false; }
            return true;
        }
        internal void Apply(float* left, float* right, int count)
        {
            for (int i = 0; i < count && ramp < rampSize; i++, ramp++)
            {
                float gain = (float)(0.5 - 0.5 * Math.Cos(Math.PI * ramp / rampSize));
                left[i] *= gain; right[i] *= gain;
            }
        }
    }
}
