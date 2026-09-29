// Compile with AudioBridgeTests.cs stubs and /main:PowerSDR.PhysicalRx2Tests.
using System;
using System.Reflection;
using System.Threading;
namespace PowerSDR
{
    internal unsafe class PhysicalRx2Tests
    {
        const int N = 2048;
        static DSP dsp = new DSP();
        static float[][] input = new float[6][], output = new float[6][];
        static int phase;
        static bool receiving = true;
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static double[] Run(string label)
        {
            double[] energy = new double[6];
            int blocks = (int)(2.8 * Audio.SampleRate1 / N) + 1, good = 0;
            for (int b = 0; b < blocks; b++)
            {
                for (int i = 0; i < N; i++, phase++)
                {
                    double a = 2 * Math.PI * 1000 * phase / Audio.SampleRate1;
                    input[0][i] = input[4][i] = (float)(1e-4 * Math.Sin(a));
                    input[1][i] = input[5][i] = (float)(1e-4 * Math.Cos(a));
                }
                fixed(float* i0=input[0], i1=input[1], i2=input[2], i3=input[3], i4=input[4], i5=input[5],
                    o0=output[0], o1=output[1], o2=output[2], o3=output[3], o4=output[4], o5=output[5])
                {
                    float** ins = stackalloc float*[6]; float** outs = stackalloc float*[6];
                    ins[0]=i0; ins[1]=i1; ins[2]=i2; ins[3]=i3; ins[4]=i4; ins[5]=i5;
                    outs[0]=o0; outs[1]=o1; outs[2]=o2; outs[3]=o3; outs[4]=o4; outs[5]=o5;
                    DspBackend.ProcessMultiChannel(ins, outs, N, receiving);
                    Check(o2[0] == -19 && o3[0] == -20, "TX pair overwritten");
                    Check(i0[100] == i4[100] && i1[100] == i5[100], "IQ modified");
                    if (!receiving) Check(o0[0] == -17 && o1[0] == -18, "TX RX1 path changed");
                    bool wdsp = dsp.Rx2.Active && (receiving || !Audio.RX2AutoMuteTX);
                    if (!wdsp) Check(o4[0] == -21 && o5[0] == -22, "inactive/muted RX2 fallback changed");
                    if (b > blocks * 0.7)
                    {
                        if (wdsp) Check(o4[0] != -21 && o5[0] != -22, "RX2 silently fell back");
                        for (int c=0; c<6; c++) for (int i=0; i<N; i++)
                        {
                            Check(!Single.IsNaN(outs[c][i]) && !Single.IsInfinity(outs[c][i]), "non-finite audio");
                            energy[c] += outs[c][i] * outs[c][i];
                        }
                        good++;
                    }
                }
                Thread.Sleep(Math.Max(1, N * 1000 / Audio.SampleRate1));
            }
            for (int c=0;c<6;c++) energy[c] = Math.Sqrt(energy[c]/(good*N));
            Console.WriteLine("{0}: RX1={1:G5} RX2 L={2:G5} R={3:G5}", label, energy[0], energy[4], energy[5]);
            return energy;
        }
        static int Main()
        {
            try
            {
                System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
                for(int c=0;c<6;c++) { input[c]=new float[N]; output[c]=new float[N]; }
                dsp.Main.Active = true; dsp.Rx2.Active = true; DspBackend.Register(dsp);
                double[] baseline = Run("independent physical RX2");
                Check(baseline[4]>1e-6, "RX2 silent");
                dsp.Rx2.Pan=0; double[] left=Run("RX2 pan left"); Check(left[5]<left[4]*1e-5,"pan");
                dsp.Rx2.RXOutputGain=0; double[] mute=Run("RX2 mute"); Check(mute[4]==0 && mute[5]==0 && mute[0]>1e-6,"independent mute");
                dsp.Rx2.RXOutputGain=1; dsp.Rx2.Pan=0.5f; dsp.Rx2.RXOsc=-5000;
                double[] tune=Run("RX2 tuning"); Check(tune[4]<baseline[4]*0.02 && tune[0]>baseline[0]*0.8,"independent tuning");
                dsp.Rx2.RXOsc=0; dsp.Rx2.SetNotch(0,1000,300,true);
                double[] notch=Run("RX2 manual notch"); Check(notch[4]<baseline[4]*0.02,"RX2 notch");
                dsp.Rx2.SetNotch(0,1000,300,false); DspBackend.SetRx2Anf(true);
                double[] anf=Run("RX2 ANF"); Check(anf[4]<baseline[4]*0.1,"RX2 ANF"); DspBackend.SetRx2Anf(false);
                dsp.Rx2.SetRXEQ(3,true,0,0,-15,0);
                double[] eq=Run("RX2 EQ"); Check(eq[4]<baseline[4]*0.5,"RX2 EQ"); dsp.Rx2.SetRXEQ(3,false,0,0,-15,0);
                dsp.Rx2.RXAGCMode=AGCMode.FAST; dsp.Rx2.RXAGCMaxGain=10; double[] low=Run("RX2 AGC 10");
                dsp.Rx2.RXAGCMaxGain=40; double[] high=Run("RX2 AGC 40"); Check(high[4]>low[4]*10,"RX2 AGC"); dsp.Rx2.RXAGCMode=AGCMode.FIXD;
                for(int nr=1;nr<=4;nr++)
                {
                    DspBackend.SetRx2NoiseReductionMode(nr); double[] result=Run("RX2 NR"+nr);
                    Check(result[0]>baseline[0]*0.8,"RX2 NR affects RX1");
                    object rx2=typeof(DspBackend).GetField("rx2",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
                    Check((int)typeof(WdspReceiver).GetField("appliedNoiseReductionMode",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(rx2)==nr,"NR not applied");
                }
                DspBackend.SetRx2NoiseReductionMode(0);
                dsp.Rx2.NBOn=true; Run("RX2 NB1"); dsp.Rx2.NBOn=false; dsp.Rx2.SDROM=true; Run("RX2 NB2"); dsp.Rx2.SDROM=false;
                DspBackend.SetDiversityScalar(-1,0); DspBackend.SetDiversity(true);
                double[] diversity=Run("diversity cancellation with RX2 WDSP");
                Check(diversity[0]<baseline[0]*0.02 && diversity[4]>baseline[4]*0.8,"diversity coupling"); DspBackend.SetDiversity(false);
                receiving=false; Audio.RX2AutoMuteTX=false;
                double[] duplex=Run("RX2 monitor during TX"); Check(duplex[4]>baseline[4]*0.8,"TX monitor");
                Audio.RX2AutoMuteTX=true; Run("RX2 auto mute TX preserves legacy path"); receiving=true;
                dsp.Rx2.Active=false; Run("RX2 disabled"); dsp.Rx2.Active=true;
                foreach(int rate in new[]{96000,192000}) { Audio.SampleRate1=rate; double[] r=Run("sample rate "+rate); Check(r[4]>1e-6,"sample rate silence"); }
                Console.WriteLine("PASS: physical RX2 routing, controls, NR1-4, NB1/2, diversity and TX isolation.");
                Environment.Exit(0); return 0;
            }
            catch(Exception ex) { Console.WriteLine(ex); Environment.Exit(1); return 1; }
        }
    }
}
