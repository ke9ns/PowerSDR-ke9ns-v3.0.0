//=================================================================
// WdspNative.cs
// Minimal interop surface for the experimental WDSP RX1 path.
//=================================================================

using System.Runtime.InteropServices;

namespace PowerSDR
{
    internal static unsafe class WdspNative
    {
        private const string DllName = "wdsp.dll";

        // WDSP uses double-precision FFTW; the legacy DttSP wisdom is a
        // different, single-precision cache and must not be overwritten.
        [DllImport("libfftw3-3.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int fftw_import_wisdom_from_string(string wisdom);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void WisdomWriter(byte character, System.IntPtr data);
        [DllImport("libfftw3-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void fftw_export_wisdom(WisdomWriter writer, System.IntPtr data);
        internal static string ExportWisdom()
        {
            // The string-returning FFTW API uses malloc, not fftw_malloc;
            // avoid cross-CRT deallocation and ANSI filesystem paths entirely.
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            WisdomWriter writer = delegate(byte c, System.IntPtr data) { text.Append((char)c); };
            fftw_export_wisdom(writer, System.IntPtr.Zero);
            System.GC.KeepAlive(writer);
            return text.ToString();
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAMSQRun(int channel, int run);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAMSQThreshold(int channel, double threshold);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAFMSQRun(int channel, int run);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAFMSQThreshold(int channel, double threshold);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void create_anbEXT(int id, int run, int size, double rate,
            double slew, double hang, double lead, double average, double threshold);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void create_nobEXT(int id, int run, int mode, int size, double rate,
            double slew, double hang, double lead, double average, double threshold);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void destroy_anbEXT(int id);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void destroy_nobEXT(int id);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void flush_anbEXT(int id);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void flush_nobEXT(int id);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void xanbEXT(int id, double* input, double* output);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void xnobEXT(int id, double* input, double* output);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAANFVals(int channel, int taps, int delay, double gain, double leak);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RXANBPAddNotch(int channel, int notch, double center, double width, int active);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RXANBPDeleteNotch(int channel, int notch);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void RXANBPSetNotchesRun(int channel, int run);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEQRun(int channel, int run);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAGrphEQ(int channel, int* values);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAGrphEQ10(int channel, int* values);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetWDSPVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void OpenChannel(int channel, int inSize, int dspSize,
            int inputSampleRate, int dspRate, int outputSampleRate, int type, int state,
            double delayUp, double slewUp, double delayDown, double slewDown, int bfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CloseChannel(int channel);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void fexchange2(int channel, float* inputI, float* inputQ,
            float* outputI, float* outputQ, int* error);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAMode(int channel, DSPMode mode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXABandpassFreqs(int channel, double low, double high);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void RXANBPSetFreqs(int channel, double low, double high);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCMode(int channel, AGCMode mode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCTop(int channel, double value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCFixed(int channel, double value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCAttack(int channel, int value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCDecay(int channel, int value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCHang(int channel, int value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCSlope(int channel, int value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAAGCHangThreshold(int channel, int value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAShiftRun(int channel, int value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAShiftFreq(int channel, double value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAPanelGain1(int channel, double value);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAPanelBinaural(int channel, int value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAANRRun(int channel, int run);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAANRPosition(int channel, int position);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAANRVals(int channel, int taps, int delay,
            double gain, double leakage);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRRun(int channel, int run);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRPosition(int channel, int position);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRgainMethod(int channel, int method);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRnpeMethod(int channel, int method);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRaeRun(int channel, int run);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRtrainZetaThresh(int channel, double threshold);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRtrainT2(int channel, double threshold);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRpost2Run(int channel, int run);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRpost2Nlevel(int channel, double level);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRpost2Factor(int channel, double factor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRpost2Rate(int channel, double rate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAEMNRpost2Taper(int channel, int taper);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXARNNRRun(int channel, int run);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXARNNRPosition(int channel, int position);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXARNNRUseDefaultGain(int channel, int useDefault);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void RNNRloadModel(string filePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRRun(int channel, int run);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRPosition(int channel, int position);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRreductionAmount(int channel, float amount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRsmoothingFactor(int channel, float factor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRwhiteningFactor(int channel, float factor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRnoiseRescale(int channel, float factor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRpostFilterThreshold(int channel, float threshold);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASBNRnoiseScalingType(int channel, int type);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXASNBARun(int channel, int run);


        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetRXAANFRun(int channel, int run);
    }
}
