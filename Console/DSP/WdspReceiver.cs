//=================================================================
// WdspReceiver.cs
// One WDSP channel and its independently controlled receiver state.
//=================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace PowerSDR
{
    internal sealed unsafe class WdspReceiver
    {
        private readonly int Rx1Channel;
        private const int MinimumWdspVersion = 129;
        private readonly object channelLock = new object();
        private readonly bool requested = true;
        private readonly WdspImpulseBlankers blankers;
        private readonly float[] iqCorrection = new float[5];
        private bool haveIqCorrection;
        private float[] correctedLeft, correctedRight;
        internal WdspReceiver(int channel) { Rx1Channel = channel; blankers = new WdspImpulseBlankers(channel); }
        private int appliedAnfTaps, appliedAnfDelay;
        private double appliedAnfGain, appliedAnfLeak;
        private int appliedNotchVersion = -1, nativeNotchCount;
        private int appliedNotchLow = Int32.MinValue, appliedNotchHigh = Int32.MinValue;
        private readonly bool[] notchActive = new bool[18];
        private readonly double[] notchFrequency = new double[18], notchWidth = new double[18];
        private int appliedEqVersion = -1;
        private readonly int[] eqValues = new int[11];
        private double appliedTop = Double.NaN, appliedFixed = Double.NaN;
        private double appliedShift = Double.NaN;
        private int appliedAttack = -1, appliedDecay = -1, appliedHang = -1;
        private int appliedSlope = -1, appliedHangThreshold = -1;
        private bool appliedBinaural;
        private bool appliedAmplitudeSquelch, appliedFmSquelch;
        private double appliedSquelchThreshold = Double.NaN, appliedFmSquelchThreshold = Double.NaN;

        private bool disabled;
        private bool channelOpen;
        private int channelBlockSize;
        private int channelSampleRate;
        private IntPtr scratchLeft = IntPtr.Zero;
        private IntPtr scratchRight = IntPtr.Zero;

        private volatile DSPMode mode = DSPMode.USB;
        private volatile AGCMode agcMode = AGCMode.MED;
        private volatile int filterLow = 150;
        private volatile int filterHigh = 2850;
        // 0=off, 1=ANR, 2=EMNR, 3=RNNoise, 4=SpecBleach.
        private volatile int noiseReductionMode;
        private volatile bool anf;

        // User-adjustable WDSP NR parameters. Values that need decimals are
        // stored as scaled integers so the audio thread can read them safely.
        private volatile int nrPosition = 1;
        private volatile int nr1Taps = 64;
        private volatile int nr1Delay = 16;
        private volatile int nr1Gain = 100;
        private volatile int nr1Leakage = 100;
        private volatile int nr2GainMethod = 2;
        private volatile int nr2NpeMethod;
        private volatile bool nr2Ae = true;
        private volatile int nr2TrainThresholdTenths = -50;
        private volatile int nr2TrainT2Hundredths = 20;
        private volatile bool nr2Post;
        private volatile int nr2PostLevel = 15;
        private volatile int nr2PostFactor = 15;
        private volatile int nr2PostRate = 15;
        private volatile int nr2PostTaper = 12;
        private volatile bool nr3FixedGain = true;
        private volatile int nr4ReductionTenths = 100;
        private volatile int nr4SmoothingTenths;
        private volatile int nr4WhiteningTenths;
        private volatile int nr4RescaleTenths = 20;
        private volatile int nr4ThresholdTenths = -100;
        private volatile int nr4Algorithm;
        private int nrConfigurationVersion;
        private int appliedNrConfigurationVersion = -1;

        private DSPMode appliedMode = DSPMode.FIRST;
        private AGCMode appliedAgc = AGCMode.FIRST;
        private int appliedLow = Int32.MinValue;
        private int appliedHigh = Int32.MinValue;
        private int appliedNoiseReductionMode = -1;
        private bool appliedAnf;
        private bool settingsApplied;

        internal bool ExperimentalWdspRequested { get { return requested; } }

        internal void SetRx1Mode(DSPMode value) { mode = value; }
        internal void SetRx1Filter(int low, int high) { filterLow = low; filterHigh = high; }
        internal void SetRx1Agc(AGCMode value) { agcMode = value; }
        internal void SetRx1NoiseReductionMode(int value)
        {
            if (value < 0) value = 0;
            if (value > 4) value = 4;
            noiseReductionMode = value;
        }
        internal void SetRx1Anf(bool value) { anf = value; }

        internal void SetRx1NrParameters(int position,
            int anrTaps, int anrDelay, int anrGain, int anrLeakage,
            int emnrGainMethod, int emnrNpeMethod, bool emnrAe,
            int emnrTrainThresholdTenths, int emnrTrainT2Hundredths,
            bool emnrPost, int emnrPostLevel, int emnrPostFactor,
            int emnrPostRate, int emnrPostTaper, bool rnnrFixedGain,
            int sbnrReductionTenths, int sbnrSmoothingTenths,
            int sbnrWhiteningTenths, int sbnrRescaleTenths,
            int sbnrThresholdTenths, int sbnrAlgorithm)
        {
            nrPosition = position;
            nr1Taps = anrTaps;
            nr1Delay = anrDelay;
            nr1Gain = anrGain;
            nr1Leakage = anrLeakage;
            nr2GainMethod = emnrGainMethod;
            nr2NpeMethod = emnrNpeMethod;
            nr2Ae = emnrAe;
            nr2TrainThresholdTenths = emnrTrainThresholdTenths;
            nr2TrainT2Hundredths = emnrTrainT2Hundredths;
            nr2Post = emnrPost;
            nr2PostLevel = emnrPostLevel;
            nr2PostFactor = emnrPostFactor;
            nr2PostRate = emnrPostRate;
            nr2PostTaper = emnrPostTaper;
            nr3FixedGain = rnnrFixedGain;
            nr4ReductionTenths = sbnrReductionTenths;
            nr4SmoothingTenths = sbnrSmoothingTenths;
            nr4WhiteningTenths = sbnrWhiteningTenths;
            nr4RescaleTenths = sbnrRescaleTenths;
            nr4ThresholdTenths = sbnrThresholdTenths;
            nr4Algorithm = sbnrAlgorithm;
            Interlocked.Increment(ref nrConfigurationVersion);
        }

        internal bool LoadRnnrModel(string filePath)
        {
            try
            {
                WdspNative.RNNRloadModel(filePath ?? String.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to load RNNoise model: " + ex);
                return false;
            }
        }

        // Called before the hardware stream starts. Native FFT planning must
        // not monopolize the first real-time audio callback for several seconds.
        internal void Prepare(DSPRX rx, int sampleCount, int sampleRate)
        {
            if (disabled || rx == null || sampleCount <= 0) return;
            try
            {
                EnsureChannel(sampleCount, sampleRate);
                mode = rx.DSPMode;
                filterLow = rx.RXFilterLow;
                filterHigh = rx.RXFilterHigh;
                agcMode = rx.RXAGCMode;
                ApplyReceiverControls(rx);
                ApplySettings();
                ApplyEqualizer(rx);
                ApplyManualNotches(rx);
            }
            catch (Exception ex) { DisableAfterFailure(ex); }
        }

        internal bool Process(DSPRX rx, float* inputLeft, float* inputRight,
            int sampleCount)
        {
            if (!requested || disabled || sampleCount <= 0 || inputLeft == null ||
                inputRight == null || rx == null) return false;

            try
            {
                EnsureChannel(sampleCount, Audio.SampleRate1);
                mode = rx.DSPMode;
                filterLow = rx.RXFilterLow;
                filterHigh = rx.RXFilterHigh;
                agcMode = rx.RXAGCMode;
                ApplyReceiverControls(rx);
                ApplySettings();
                ApplyEqualizer(rx);
                ApplyManualNotches(rx);

                int error = 0;
                // Keep legacy buffer order: WDSP's complex FIR uses the
                // opposite frequency sign to DttSP (see fir_bandpass).
                bool blanked = blankers.Process(inputLeft, inputRight, sampleCount, Audio.SampleRate1,
                    rx.NBOn, rx.SDROM, DspBackend.Nb1Settings, DspBackend.Nb2Settings);
                // DttSP still learns WBIR coefficients for the spectrum/legacy
                // path. Use a coherent snapshot of those exact two stages,
                // after NB and before WDSP frequency shift and resampling.
                // Original input belongs to DttSP, RX-S and diversity: never edit it.
                if (rx.TryGetRXIQCorrection(iqCorrection)) haveIqCorrection = true;
                if (!haveIqCorrection) return false;
                if (correctedLeft == null || correctedLeft.Length != sampleCount)
                { correctedLeft = new float[sampleCount]; correctedRight = new float[sampleCount]; }
                for (int k = 0; k < iqCorrection.Length; k++)
                    if (Single.IsNaN(iqCorrection[k]) || Single.IsInfinity(iqCorrection[k]))
                        throw new InvalidOperationException("Invalid legacy RX IQ correction.");
                for (int i = 0; i < sampleCount; i++)
                {
                    // DttSP process_samples uses real=RIGHT, imag=LEFT.
                    // Its calibration coefficients belong to that convention.
                    float re = blanked ? blankers.Right[i] : inputRight[i];
                    float im = blanked ? blankers.Left[i] : inputLeft[i];
                    if (iqCorrection[0] != 0)
                    {
                        float r = re + iqCorrection[1] * re + iqCorrection[2] * im;
                        float q = im + iqCorrection[2] * re - iqCorrection[1] * im;
                        re = r + iqCorrection[3] * r + iqCorrection[4] * q;
                        im = q + iqCorrection[4] * r - iqCorrection[3] * q;
                    }
                    // Restore the bridge's existing L/R order for WDSP.
                    // Do not change the oscillator/sideband convention.
                    correctedLeft[i] = im;
                    correctedRight[i] = re;
                }
                fixed (float* l = correctedLeft, r = correctedRight)
                    WdspNative.fexchange2(Rx1Channel, l, r,
                        (float*)scratchLeft, (float*)scratchRight, &error);
                // -2 means the nonblocking WDSP output queue is temporarily
                // empty; fexchange2 has already zeroed BOTH scratch buffers.
                // Do not insert an unrelated DttSP audio block (different delay,
                // gain and NR) into this stream during a scheduling hiccup.
                return error == 0 || error == -2;
            }
            catch (Exception ex)
            {
                DisableAfterFailure(ex);
                return false;
            }
        }

        // Commit to the caller only after every active receiver succeeded, so a
        // failure preserves the complete DttSP mix instead of half a stereo pair.
        internal void Mix(DSPRX rx, float* left, float* right, int count, bool add,
            bool multipleReceivers)
        {
            double pan = Math.Max(0, Math.Min(1, rx.Pan));
            double gain = rx.RXOutputGain * (multipleReceivers ? 1.0 : 1.414);
            float gl = (float)(gain * (pan <= 0.5 ? 1 : Math.Cos((pan - 0.5) * Math.PI)));
            float gr = (float)(gain * (pan >= 0.5 ? 1 : Math.Sin(pan * Math.PI)));
            for (int i = 0; i < count; i++)
            {
                float l = ((float*)scratchLeft)[i] * gl;
                float r = ((float*)scratchRight)[i] * gr;
                left[i] = (add ? left[i] : 0) + l;
                right[i] = (add ? right[i] : 0) + r;
            }
        }

        private void ApplyReceiverControls(DSPRX rx)
        {
            bool reset = !settingsApplied;
            bool fmSquelch = rx.RXSquelchOn && rx.DSPMode == DSPMode.FM;
            bool amplitudeSquelch = rx.RXSquelchOn && rx.DSPMode != DSPMode.FM;
            // The UI already includes the radio's meter/preamp/path calibration.
            // DttSP compares summed block power; WDSP compares mean amplitude.
            double squelchThreshold = rx.RXSquelchThreshold -
                10.0 * Math.Log10(Math.Max(1, rx.BufferSize));
            if (reset || squelchThreshold != appliedSquelchThreshold)
                WdspNative.SetRXAAMSQThreshold(Rx1Channel, appliedSquelchThreshold = squelchThreshold);
            if (reset || rx.FMSquelchThreshold != appliedFmSquelchThreshold)
                WdspNative.SetRXAFMSQThreshold(Rx1Channel, appliedFmSquelchThreshold = rx.FMSquelchThreshold);
            if (reset || amplitudeSquelch != appliedAmplitudeSquelch)
            {
                WdspNative.SetRXAAMSQRun(Rx1Channel, amplitudeSquelch ? 1 : 0);
                appliedAmplitudeSquelch = amplitudeSquelch;
            }
            if (reset || fmSquelch != appliedFmSquelch)
            {
                WdspNative.SetRXAFMSQRun(Rx1Channel, fmSquelch ? 1 : 0);
                appliedFmSquelch = fmSquelch;
            }
            if (reset || rx.ANFTaps != appliedAnfTaps || rx.ANFDelay != appliedAnfDelay ||
                rx.ANFGain != appliedAnfGain || rx.ANFLeak != appliedAnfLeak)
                WdspNative.SetRXAANFVals(Rx1Channel, appliedAnfTaps = rx.ANFTaps,
                    appliedAnfDelay = rx.ANFDelay, appliedAnfGain = rx.ANFGain, appliedAnfLeak = rx.ANFLeak);
            if (reset || rx.BinOn != appliedBinaural)
            {
                appliedBinaural = rx.BinOn;
                WdspNative.SetRXAPanelBinaural(Rx1Channel, appliedBinaural ? 1 : 0);
            }
            bool modeChange = reset || rx.RXAGCMode != appliedAgc;
            if (modeChange) WdspNative.SetRXAAGCMode(Rx1Channel, rx.RXAGCMode);
            if (reset || rx.RXAGCMaxGain != appliedTop)
                WdspNative.SetRXAAGCTop(Rx1Channel, appliedTop = rx.RXAGCMaxGain);
            if (reset || rx.RXFixedAGC != appliedFixed)
                WdspNative.SetRXAAGCFixed(Rx1Channel, appliedFixed = rx.RXFixedAGC);
            if (modeChange || rx.RXAGCAttack != appliedAttack)
                WdspNative.SetRXAAGCAttack(Rx1Channel, appliedAttack = rx.RXAGCAttack);
            if (modeChange || rx.RXAGCDecay != appliedDecay)
                WdspNative.SetRXAAGCDecay(Rx1Channel, appliedDecay = rx.RXAGCDecay);
            if (modeChange || rx.RXAGCHang != appliedHang)
                WdspNative.SetRXAAGCHang(Rx1Channel, appliedHang = rx.RXAGCHang);
            if (reset || rx.RXAGCSlope != appliedSlope)
                WdspNative.SetRXAAGCSlope(Rx1Channel, appliedSlope = rx.RXAGCSlope);
            if (modeChange || rx.RXAGCHangThreshold != appliedHangThreshold)
                WdspNative.SetRXAAGCHangThreshold(Rx1Channel,
                    appliedHangThreshold = rx.RXAGCHangThreshold);
            if (reset || rx.RXOsc != appliedShift)
            {
                appliedShift = rx.RXOsc;
                WdspNative.SetRXAShiftFreq(Rx1Channel, -appliedShift);
                WdspNative.SetRXAShiftRun(Rx1Channel, appliedShift == 0 ? 0 : 1);
            }
        }

        private void ApplyManualNotches(DSPRX rx)
        {
            int version = rx.NotchVersion;
            int low = rx.RXFilterLow, high = rx.RXFilterHigh;
            if (version == appliedNotchVersion && low == appliedNotchLow && high == appliedNotchHigh)
                return;
            version = rx.CopyNotches(notchActive, notchFrequency, notchWidth);

            // PowerSDR stores TNFs as demodulated audio frequencies. WDSP's
            // NBP runs before demodulation, so choose the sign(s) present in
            // the current complex passband. Symmetric modes receive both.
            WdspNative.RXANBPSetNotchesRun(Rx1Channel, 0);
            while (nativeNotchCount > 0)
            {
                WdspNative.RXANBPDeleteNotch(Rx1Channel, 0);
                nativeNotchCount--;
            }
            for (uint i = 0; i < 18; i++)
            {
                if (!notchActive[i]) continue;
                double frequency = Math.Abs(notchFrequency[i]);
                double width = Math.Max(1.0, notchWidth[i]);
                bool positive = frequency >= low && frequency <= high;
                bool negative = -frequency >= low && -frequency <= high;
                if (positive && WdspNative.RXANBPAddNotch(Rx1Channel, nativeNotchCount,
                    frequency, width, 1) == 0) nativeNotchCount++;
                if (negative && frequency != 0 && WdspNative.RXANBPAddNotch(Rx1Channel,
                    nativeNotchCount, -frequency, width, 1) == 0) nativeNotchCount++;
            }
            WdspNative.RXANBPSetNotchesRun(Rx1Channel, nativeNotchCount > 0 ? 1 : 0);
            appliedNotchLow = low;
            appliedNotchHigh = high;
            // If the UI changed the three fields while this snapshot was being
            // built, leave it dirty and rebuild the coherent state next block.
            appliedNotchVersion = rx.NotchVersion == version ? version : -1;
        }

        private void ApplyEqualizer(DSPRX rx)
        {
            int version = rx.RXEQVersion;
            if (version == appliedEqVersion) return;
            int bands; bool enabled;
            version = rx.CopyRXEQ(eqValues, out bands, out enabled);
            fixed (int* values = eqValues)
            {
                if (bands == 3) WdspNative.SetRXAGrphEQ(Rx1Channel, values);
                else WdspNative.SetRXAGrphEQ10(Rx1Channel, values);
            }
            WdspNative.SetRXAEQRun(Rx1Channel, enabled ? 1 : 0);
            appliedEqVersion = rx.RXEQVersion == version ? version : -1;
        }

        private void EnsureChannel(int sampleCount, int sampleRate)
        {
            if (channelOpen && channelBlockSize == sampleCount && channelSampleRate == sampleRate) return;

            lock (channelLock)
            {
                if (channelOpen && channelBlockSize == sampleCount && channelSampleRate == sampleRate) return;
                if (channelOpen) WdspNative.CloseChannel(Rx1Channel);
                FreeScratch();

                int version = WdspNative.GetWDSPVersion();
                if (version < MinimumWdspVersion)
                    throw new InvalidOperationException("WDSP version " + version + " is too old.");

                scratchLeft = Marshal.AllocHGlobal(sampleCount * sizeof(float));
                scratchRight = Marshal.AllocHGlobal(sampleCount * sizeof(float));
                int dspSize = sampleCount < 2048 ? 2048 : sampleCount;
                // RNNoise frames are defined at 48 kHz. WDSP resamples the
                // radio IQ input and restores its original output rate.
                WdspNative.OpenChannel(Rx1Channel, sampleCount, dspSize, sampleRate,
                    48000, sampleRate, 0, 1, 0.0, 0.01, 0.0, 0.01, 0);
                WdspNative.SetRXAPanelGain1(Rx1Channel, 1.0);

                // SNB is intentionally excluded from this bridge. NB controls
                // must never enable spectral blanking as a substitute for NB1/NB2.
                WdspNative.SetRXASNBARun(Rx1Channel, 0);

                channelBlockSize = sampleCount;
                channelSampleRate = sampleRate;
                channelOpen = true;
                haveIqCorrection = false;
                settingsApplied = false;
                appliedNotchVersion = -1;
                appliedEqVersion = -1;
                nativeNotchCount = 0;
                Debug.WriteLine("WDSP channel " + Rx1Channel + " active. Version=" + version +
                    " Rate=" + sampleRate + " Block=" + sampleCount);
            }
        }

        private void ApplySettings()
        {
            DSPMode wantedMode = mode;
            AGCMode wantedAgc = agcMode;
            int wantedLow = filterLow;
            int wantedHigh = filterHigh;
            int wantedNoiseReductionMode = noiseReductionMode;
            bool wantedAnf = anf;

            bool modeChanged = !settingsApplied || wantedMode != appliedMode;
            bool filterChanged = !settingsApplied || wantedLow != appliedLow || wantedHigh != appliedHigh;
            bool nrChanged = !settingsApplied || wantedNoiseReductionMode != appliedNoiseReductionMode;
            bool anfChanged = !settingsApplied || wantedAnf != appliedAnf;
            int wantedNrConfigurationVersion = nrConfigurationVersion;
            bool nrParametersChanged = !settingsApplied ||
                wantedNrConfigurationVersion != appliedNrConfigurationVersion;

            if (modeChanged)
                WdspNative.SetRXAMode(Rx1Channel, wantedMode);
            if (nrParametersChanged)
            {
                int position = nrPosition;
                WdspNative.SetRXAANRPosition(Rx1Channel, position);
                WdspNative.SetRXAANRVals(Rx1Channel, nr1Taps, nr1Delay,
                    nr1Gain * 1.0e-6, nr1Leakage * 1.0e-3);

                WdspNative.SetRXAEMNRPosition(Rx1Channel, position);
                WdspNative.SetRXAEMNRgainMethod(Rx1Channel, nr2GainMethod);
                WdspNative.SetRXAEMNRnpeMethod(Rx1Channel, nr2NpeMethod);
                WdspNative.SetRXAEMNRaeRun(Rx1Channel, nr2Ae ? 1 : 0);
                WdspNative.SetRXAEMNRtrainZetaThresh(Rx1Channel,
                    nr2TrainThresholdTenths / 10.0);
                WdspNative.SetRXAEMNRtrainT2(Rx1Channel,
                    nr2TrainT2Hundredths / 100.0);
                WdspNative.SetRXAEMNRpost2Nlevel(Rx1Channel, nr2PostLevel);
                WdspNative.SetRXAEMNRpost2Factor(Rx1Channel, nr2PostFactor);
                WdspNative.SetRXAEMNRpost2Rate(Rx1Channel, nr2PostRate);
                WdspNative.SetRXAEMNRpost2Taper(Rx1Channel, nr2PostTaper);
                WdspNative.SetRXAEMNRpost2Run(Rx1Channel, nr2Post ? 1 : 0);

                WdspNative.SetRXARNNRPosition(Rx1Channel, position);
                WdspNative.SetRXARNNRUseDefaultGain(Rx1Channel, nr3FixedGain ? 1 : 0);

                WdspNative.SetRXASBNRPosition(Rx1Channel, position);
                WdspNative.SetRXASBNRreductionAmount(Rx1Channel,
                    nr4ReductionTenths / 10.0f);
                WdspNative.SetRXASBNRsmoothingFactor(Rx1Channel,
                    nr4SmoothingTenths / 10.0f);
                WdspNative.SetRXASBNRwhiteningFactor(Rx1Channel,
                    nr4WhiteningTenths / 10.0f);
                WdspNative.SetRXASBNRnoiseRescale(Rx1Channel,
                    nr4RescaleTenths / 10.0f);
                WdspNative.SetRXASBNRpostFilterThreshold(Rx1Channel,
                    nr4ThresholdTenths / 10.0f);
                WdspNative.SetRXASBNRnoiseScalingType(Rx1Channel, nr4Algorithm);
            }
            if (nrChanged)
            {
                // Match Thetis: exactly one NR engine runs at a time, after AGC.
                WdspNative.SetRXAANRRun(Rx1Channel, 0);
                WdspNative.SetRXAEMNRRun(Rx1Channel, 0);
                WdspNative.SetRXARNNRRun(Rx1Channel, 0);
                WdspNative.SetRXASBNRRun(Rx1Channel, 0);

                switch (wantedNoiseReductionMode)
                {
                    case 1: WdspNative.SetRXAANRRun(Rx1Channel, 1); break;
                    case 2: WdspNative.SetRXAEMNRRun(Rx1Channel, 1); break;
                    case 3: WdspNative.SetRXARNNRRun(Rx1Channel, 1); break;
                    case 4: WdspNative.SetRXASBNRRun(Rx1Channel, 1); break;
                }
            }
            if (anfChanged)
                WdspNative.SetRXAANFRun(Rx1Channel, wantedAnf ? 1 : 0);

            // Keep the active passband-related stages synchronized.
            // Topology changes made by NR/ANF can rebuild or reposition those
            // stages, so apply the user's exact filter edges last every time.
            if (filterChanged || modeChanged || nrChanged || anfChanged ||
                nrParametersChanged)
            {
                WdspNative.RXANBPSetFreqs(Rx1Channel, wantedLow, wantedHigh);
                WdspNative.SetRXABandpassFreqs(Rx1Channel, wantedLow, wantedHigh);
            }

            appliedMode = wantedMode;
            appliedAgc = wantedAgc;
            appliedLow = wantedLow;
            appliedHigh = wantedHigh;
            appliedNoiseReductionMode = wantedNoiseReductionMode;
            appliedAnf = wantedAnf;
            appliedNrConfigurationVersion = wantedNrConfigurationVersion;
            settingsApplied = true;
        }

        private void DisableAfterFailure(Exception ex)
        {
            disabled = true;
            Debug.WriteLine("WDSP channel " + Rx1Channel + " disabled; DttSP fallback remains active: " + ex);
            try { if (channelOpen) WdspNative.CloseChannel(Rx1Channel); } catch { }
            channelOpen = false;
            FreeScratch();
        }

        private void FreeScratch()
        {
            blankers.Dispose();
            if (scratchLeft != IntPtr.Zero) Marshal.FreeHGlobal(scratchLeft);
            if (scratchRight != IntPtr.Zero) Marshal.FreeHGlobal(scratchRight);
            scratchLeft = IntPtr.Zero;
            scratchRight = IntPtr.Zero;
        }
    }
}
