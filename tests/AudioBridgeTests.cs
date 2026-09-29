// Standalone x86 integration harness: real WDSP, stubbed hardware/DttSP.
using System;
using System.Threading;
namespace PowerSDR
{
    public enum DSPMode { FIRST=-1, LSB, USB, DSB, CWL, CWU, FM, AM, DIGU, SPEC, DIGL, SAM, DRM }
    public enum AGCMode { FIRST=-1, FIXD, LONG, SLOW, MED, FAST, CUSTOM }
    public static class Audio { public static int SampleRate1=48000; public static bool RX2AutoMuteTX=true; }
    public class DSPRX
    {
        public DSPMode DSPMode=DSPMode.USB;
        public AGCMode RXAGCMode=AGCMode.FIXD;
        public int RXFilterLow=150, RXFilterHigh=2850;
        public double RXAGCMaxGain=40, RXFixedAGC=0, RXOsc=0, RXOutputGain=1;
        public int RXAGCAttack=2, RXAGCDecay=100, RXAGCHang=100, RXAGCSlope=0, RXAGCHangThreshold=0;
        public float Pan=0.5f;
        public bool Active, BinOn;
        public bool RXSquelchOn;
        public int BufferSize=2048;
        public float RXSquelchThreshold=-150, FMSquelchThreshold=1;
        public bool NBOn, SDROM;
        public bool RXEQOn;
        public int RXEQNumBands=3;
        public int RXEQVersion { get { return rxEqVersion; } }
        public int ANFTaps=64, ANFDelay=64;
        public double ANFGain=0.001, ANFLeak=1e-7;
        bool[] notchOn=new bool[18]; double[] notchFreq=new double[18], notchBw=new double[18]; int notchVersion;
        int[] rxEq3=new int[4], rxEq10=new int[11]; int rxEqVersion;
        public int NotchVersion { get { return notchVersion; } }
        public bool GetNotchOn(int i) { return notchOn[i]; }
        public double GetNotchFreq(uint i) { return notchFreq[i]; }
        public double GetNotchBW(uint i) { return notchBw[i]; }
        public int CopyNotches(bool[] active, double[] frequencies, double[] widths)
        { Array.Copy(notchOn,active,18);Array.Copy(notchFreq,frequencies,18);Array.Copy(notchBw,widths,18);return notchVersion; }
        public void SetNotch(int i, double freq, double width, bool on)
        { notchFreq[i]=freq; notchBw[i]=width; notchOn[i]=on; notchVersion++; }
        public int CopyRXEQ(int[] values, out int bands, out bool enabled)
        { bands=RXEQNumBands;enabled=RXEQOn;Array.Clear(values,0,values.Length);Array.Copy(bands==3?rxEq3:rxEq10,values,bands==3?4:11);return rxEqVersion; }
        public void SetRXEQ(int bands, bool enabled, params int[] values)
        { RXEQNumBands=bands;RXEQOn=enabled;Array.Clear(bands==3?rxEq3:rxEq10,0,bands==3?4:11);Array.Copy(values,bands==3?rxEq3:rxEq10,Math.Min(values.Length,bands==3?4:11));rxEqVersion++; }
    }
    public class DSP
    {
        public DSPRX Main=new DSPRX(), Sub=new DSPRX(), Rx2=new DSPRX(), Rx2Sub=new DSPRX();
        public DSPRX GetDSPRX(int a,int b) { return a==1 ? (b==0 ? Rx2 : Rx2Sub) : (b==0 ? Main : Sub); }
    }
    public unsafe class DttSP
    {
        public static void ExchangeSamples(void* il,void* ir,void* ol,void* oright,int n)
        { for(int i=0;i<n;i++) { ((float*)ol)[i]=-17; ((float*)oright)[i]=-17; } }
        public static void ExchangeSamples2(void* input,void* output,int n)
        { for(int c=0;c<6;c++) for(int i=0;i<n;i++) ((float**)output)[c][i]=-17-c; }
    }
    internal unsafe class AudioBridgeTests
    {
        const int N=2048;
        static DSP dsp=new DSP();
        static float[][] inputs=new float[6][], outputs=new float[6][];
        static int phase;
        static double signalHz=1000;
        static double[] Run(string label)
        {
            double l=0,r=0; int good=0, fallback=0;
            for(int b=0;b<65;b++)
            {
                for(int i=0;i<N;i++,phase++)
                {
                    double a=2*Math.PI*signalHz*phase/48000;
                    inputs[0][i]=inputs[4][i]=(float)(1e-4*Math.Sin(a));
                    inputs[1][i]=inputs[5][i]=(float)(1e-4*Math.Cos(a));
                }
                fixed(float* i0=inputs[0],i1=inputs[1],i2=inputs[2],i3=inputs[3],i4=inputs[4],i5=inputs[5],
                    o0=outputs[0],o1=outputs[1],o2=outputs[2],o3=outputs[3],o4=outputs[4],o5=outputs[5])
                {
                    float** ip=stackalloc float*[6]; float** op=stackalloc float*[6];
                    ip[0]=i0;ip[1]=i1;ip[2]=i2;ip[3]=i3;ip[4]=i4;ip[5]=i5;
                    op[0]=o0;op[1]=o1;op[2]=o2;op[3]=o3;op[4]=o4;op[5]=o5;
                    DspBackend.ProcessMultiChannel(ip,op,N,true);
                    Check(o2[0]==-19 && o3[0]==-20 && o4[0]==-21 && o5[0]==-22,"TX and physical RX2 preserved");
                    Check(i0[10]==i4[10] && i1[10]==i5[10],"original IQ not modified");
                    if(b>40)
                    {
                        if(o0[0]==-17) fallback++;
                        else { for(int i=0;i<N;i++) { l+=o0[i]*o0[i];r+=o1[i]*o1[i]; } good++; }
                    }
                }
                Thread.Sleep(43);
            }
            Check(good>15,"WDSP produces output: "+label);
            l=Math.Sqrt(l/(good*N));r=Math.Sqrt(r/(good*N));
            Console.WriteLine("{0}: L={1:G6} R={2:G6} fallback={3}",label,l,r,fallback);
            return new double[]{l,r};
        }
        static void Check(bool ok,string label) { if(!ok) throw new Exception(label); }
        static void Main()
        {
            try { TestMain(); Environment.Exit(0); }
            catch(Exception ex) { Console.WriteLine(ex); Environment.Exit(1); }
        }
        static void TestMain()
        {
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            Environment.SetEnvironmentVariable("POWERSDR_WDSP_RX1", null);
            for(int c=0;c<6;c++) { inputs[c]=new float[N];outputs[c]=new float[N]; }
            dsp.Main.Active=true; DspBackend.Register(dsp);
            double[] baseline=Run("mono centered"); Check(baseline[0]>1e-6 && Math.Abs(baseline[0]-baseline[1])<1e-9,"mono both outputs");
            dsp.Main.Pan=0; double[] left=Run("pan left"); Check(left[1]<left[0]*1e-5,"left only");
            dsp.Main.Pan=1; double[] right=Run("pan right"); Check(right[0]<right[1]*1e-5,"right only");
            dsp.Main.Pan=0.5f; dsp.Main.RXOutputGain=0; double[] mute=Run("RX-M mute"); Check(mute[0]==0 && mute[1]==0,"mute");
            dsp.Sub.Active=true; dsp.Sub.Pan=1; double[] sub=Run("RX-S independent"); Check(sub[1]>1e-6 && sub[0]<sub[1]*1e-5,"subreceiver");
            dsp.Sub.RXOsc=-5000; double[] shift=Run("RX-S out of passband"); Check(shift[1]<sub[1]*0.02,"independent tuning/filter");
            signalHz=6000; double[] tuned=Run("RX-S 6 kHz shifted to 1 kHz"); Check(tuned[1]>sub[1]*0.8,"oscillator sign");
            signalHz=1000;
            dsp.Sub.Active=false; dsp.Main.RXOutputGain=1;
            dsp.Main.RXAGCMode=AGCMode.FAST; dsp.Main.RXAGCMaxGain=10;
            double[] low=Run("AGC top 10 dB"); dsp.Main.RXAGCMaxGain=40;
            double[] high=Run("AGC top 40 dB"); Check(high[0]>low[0]*10,"AGC threshold affects audio");
            dsp.Main.RXAGCMode=AGCMode.FIXD;
            double[] tone=Run("tone before manual notch");
            dsp.Main.SetRXEQ(3,true,0,0,-15,0);
            double[] eqCut=Run("RX EQ mid cut at 1 kHz");
            Check(eqCut[0]<tone[0]*0.5,"RX EQ attenuation");
            dsp.Main.SetRXEQ(3,false,0,0,-15,0);
            double[] eqOff=Run("RX EQ off");
            Check(eqOff[0]>tone[0]*0.8,"RX EQ recovery");
            dsp.Main.SetNotch(0,1000,300,true);
            double[] manual=Run("manual notch at 1 kHz");
            Check(manual[0]<tone[0]*0.02,"manual notch attenuation");
            dsp.Main.SetNotch(0,1000,300,false);
            double[] restoredTone=Run("manual notch off");
            Check(restoredTone[0]>tone[0]*0.8,"manual notch recovery");
            dsp.Main.DSPMode=DSPMode.LSB; dsp.Main.RXFilterLow=-2850; dsp.Main.RXFilterHigh=-150; signalHz=-1000;
            double[] lsbTone=Run("LSB tone before notch");
            dsp.Main.SetNotch(0,1000,300,true);
            double[] lsbNotch=Run("LSB manual notch"); Check(lsbNotch[0]<lsbTone[0]*0.02,"LSB notch sign");
            dsp.Main.SetNotch(0,1000,300,false);
            dsp.Main.DSPMode=DSPMode.DSB; dsp.Main.RXFilterLow=-2850; dsp.Main.RXFilterHigh=2850; signalHz=1000;
            dsp.Main.SetNotch(0,1000,300,true);
            double[] dsbPositive=Run("DSB positive-side notch");
            signalHz=-1000; double[] dsbNegative=Run("DSB negative-side notch");
            Check(dsbPositive[0]<tone[0]*0.02 && dsbNegative[0]<tone[0]*0.02,"symmetric notch signs");
            dsp.Main.SetNotch(0,1000,300,false);
            dsp.Main.DSPMode=DSPMode.USB; dsp.Main.RXFilterLow=150; dsp.Main.RXFilterHigh=2850; signalHz=1000;
            restoredTone=Run("tone restored after sideband tests");
            dsp.Main.ANFTaps=68; dsp.Main.ANFDelay=60; dsp.Main.ANFGain=0.0025; dsp.Main.ANFLeak=1e-7;
            DspBackend.SetRx1Anf(true);
            double[] automatic=Run("automatic notch at 1 kHz");
            Check(automatic[0]<restoredTone[0]*0.1,"automatic notch attenuation");
            DspBackend.SetRx1Anf(false);
            double[] anfOff=Run("automatic notch off");
            Check(anfOff[0]>restoredTone[0]*0.8,"automatic notch recovery");
            dsp.Main.RXOutputGain=0; dsp.Sub.Active=true; dsp.Sub.Pan=0.5f;
            dsp.Sub.RXOsc=0; dsp.Sub.DSPMode=DSPMode.USB; dsp.Sub.RXFilterLow=150; dsp.Sub.RXFilterHigh=2850;
            double[] subTone=Run("RX-S tone before notch");
            dsp.Sub.SetNotch(0,1000,300,true);
            double[] subManual=Run("RX-S manual notch");
            Check(subManual[0]<subTone[0]*0.02,"RX-S manual notch attenuation");
            dsp.Sub.SetNotch(0,1000,300,false); DspBackend.SetRx1Anf(true);
            double[] subAutomatic=Run("RX-S automatic notch");
            Check(subAutomatic[0]<subTone[0]*0.1,"RX-S automatic notch attenuation");
            DspBackend.SetRx1Anf(false); dsp.Sub.Active=false; dsp.Main.RXOutputGain=1;
            DspBackend.SetDiversityScalar(-1,0); DspBackend.SetDiversity(true);
            double[] cancel=Run("diversity cancel"); Check(cancel[0]<baseline[0]*0.001,"diversity null");
            DspBackend.SetDiversity(false); double[] restore=Run("diversity off"); Check(restore[0]>baseline[0]*0.8,"diversity restores RX1");
            dsp.Main.NBOn=true; double[] nb1=Run("NB1 clean tone"); Check(nb1[0]>baseline[0]*0.8,"NB1 passes clean signal");
            dsp.Main.NBOn=false; dsp.Main.SDROM=true;
            double[] nb2=Run("NB2 clean tone"); Check(nb2[0]>baseline[0]*0.8,"NB2 passes clean signal");
            dsp.Main.NBOn=true;
            for(int nr=1;nr<=4;nr++) { DspBackend.SetRx1NoiseReductionMode(nr); Run("NR"+nr+" + NB1/NB2"); }
            Console.WriteLine("PASS: RX EQ, manual/automatic notches, sideband signs, RX-S, routing, AGC, diversity, NBs/NRs, and preserved RX2/TX buffers.");
        }
    }
}
