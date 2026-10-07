using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
namespace PowerSDR
{
    // Real WDSP, synthetic weak voiced signal, stubbed DttSP/hardware.
    internal unsafe class NrResumeTests
    {
        const int N=2048, Rate=192000;
        static float[] l=new float[N], r=new float[N], ol=new float[N], orr=new float[N];
        static long sample;
        static double Block(bool receiving, bool relayImpulse = false)
        {
            for(int i=0;i<N;i++,sample++)
            {
                double t=(double)sample/Rate, v=0;
                for(int k=1;k<=16;k++) v+=Math.Sin(2*Math.PI*(k*130)*t)/k;
                l[i]=relayImpulse ? 0.75f : (float)(1e-4*v*(0.65+0.35*Math.Sin(2*Math.PI*3*t)));
                r[i]=0;
            }
            fixed(float* a=l,b=r,c=ol,d=orr) DspBackend.ProcessStereo(a,b,c,d,N,receiving);
            double energy=0;
            if(receiving && ol[0]==-17) throw new Exception("Legacy fallback instead of WDSP");
            for(int i=0;i<N;i++)
            {
                if(float.IsNaN(ol[i])||float.IsInfinity(ol[i])) throw new Exception("Nonfinite audio");
                energy+=ol[i]*ol[i];
            }
            Thread.Sleep(11);
            return Math.Sqrt(energy/N);
        }
        static int Main()
        {
            try
            {
                Audio.SampleRate1=Rate;
                DSP dsp=new DSP(); dsp.Main.Active=true;
                dsp.Main.DSPMode=DSPMode.DSB;
                dsp.Main.RXFilterLow=-3000; dsp.Main.RXFilterHigh=3000;
                dsp.Main.RXAGCMode=AGCMode.MED; dsp.Main.RXAGCMaxGain=80;
                DspBackend.Register(dsp);
                Stopwatch prepare=Stopwatch.StartNew();
                DspBackend.PrepareForAudio(N,Rate,Environment.CurrentDirectory);
                Console.WriteLine("WDSP preparation (cached on next process): {0} ms",prepare.ElapsedMilliseconds);
                object receiver=typeof(DspBackend).GetField("main",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                FieldInfo open=typeof(WdspReceiver).GetField("channelOpen",BindingFlags.Instance|BindingFlags.NonPublic);
                FieldInfo settings=typeof(WdspReceiver).GetField("settingsApplied",BindingFlags.Instance|BindingFlags.NonPublic);
                FieldInfo applied=typeof(WdspReceiver).GetField("appliedNoiseReductionMode",BindingFlags.Instance|BindingFlags.NonPublic);
                for(int nr=0;nr<=4;nr++)
                {
                    DspBackend.SetRx1NoiseReductionMode(nr);
                    double pre=0;
                    Stopwatch init=Stopwatch.StartNew(); Block(true);
                    Console.WriteLine("NR{0} first callback wall time: {1} ms",nr,init.ElapsedMilliseconds);
                    for(int b=0;b<350;b++) { double rms=Block(true); if(b>=300)pre+=rms/50; }
                    DspBackend.BeginRadioTransition(true);
                    for(int b=0;b<100;b++) Block(false);
                    // Audio flag has returned to RX but the hardware has not.
                    for(int b=0;b<10;b++)
                        if(Block(true,true)!=0) throw new Exception("RX leaked before hardware completion");
                    if(!(bool)open.GetValue(receiver) || !(bool)settings.GetValue(receiver) || (int)applied.GetValue(receiver)!=nr)
                        throw new Exception("TX reset channel or NR settings");
                    DspBackend.CompleteReceiveTransition(50);
                    for(int b=0;b<5;b++)
                        if(Block(true,true)!=0) throw new Exception("RX leaked during relay settling");
                    double first=0,late=0;
                    for(int b=0;b<200;b++)
                    {
                        double rms=Block(true);
                        if(b<10) first+=rms/10;
                        if(b>=150)late+=rms/50;
                    }
                    Console.WriteLine("NR{0}: pre={1:G6}, first 107ms={2:G6}, late={3:G6}, first/pre={4:F3}",nr,pre,first,late,first/pre);
                }
                Console.WriteLine("PASS: NR0-4 pause/resume at 192 kHz: channels/settings retained; no fallback or invalid audio.");
                Environment.Exit(0); return 0;
            }
            catch(Exception ex) { Console.WriteLine(ex); Environment.Exit(1); return 1; }
        }
    }
}
