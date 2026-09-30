using System;
using System.Threading;
namespace PowerSDR
{
    internal unsafe class SquelchTests
    {
        const int N=2048;
        static DSP dsp=new DSP();
        static WdspReceiver[] channels={new WdspReceiver(0),new WdspReceiver(1),new WdspReceiver(2)};
        static DSPRX[] receivers;
        static float[] il=new float[N], ir=new float[N], ol=new float[N], oright=new float[N];
        static long sample;
        static bool noise;
        static bool fmVoice;
        static Random random=new Random(1234);
        static void Check(bool ok,string name) { if(!ok) throw new Exception(name); }
        static double[] Run(string label)
        {
            double[] energy=new double[3]; int count=0;
            int blocks=(int)(3.0*Audio.SampleRate1/N);
            for(int b=0;b<blocks;b++)
            {
                for(int i=0;i<N;i++,sample++)
                {
                    double phase=2*Math.PI*1000*sample/Audio.SampleRate1;
                    if(fmVoice) phase=-1.5*Math.Cos(phase); // clean 1 kHz FM audio, 1.5 kHz deviation
                    il[i]=noise ? (float)((random.NextDouble()*2-1)*1e-4) : (float)(1e-4*Math.Sin(phase));
                    ir[i]=noise ? (float)((random.NextDouble()*2-1)*1e-4) : (float)(1e-4*Math.Cos(phase));
                }
                fixed(float* l=il,r=ir,o=ol,p=oright)
                    for(int c=0;c<3;c++)
                    {
                        Check(channels[c].Process(receivers[c],l,r,N),"WDSP fallback");
                        channels[c].Mix(receivers[c],o,p,N,false,false);
                        if(b>blocks*0.75)
                            for(int i=0;i<N;i++) { Check(!Single.IsNaN(o[i])&&!Single.IsInfinity(o[i]),"nonfinite"); energy[c]+=o[i]*o[i]; }
                    }
                if(b>blocks*0.75) count+=N;
                Thread.Sleep(Math.Max(1,N*1000/Audio.SampleRate1));
            }
            for(int c=0;c<3;c++) energy[c]=Math.Sqrt(energy[c]/count);
            Console.WriteLine(label+": RX1="+energy[0]+" RX-S="+energy[1]+" RX2="+energy[2]);
            return energy;
        }
        static int Main()
        {
            try
            {
                receivers=new[]{dsp.Main,dsp.Sub,dsp.Rx2};
                double[] off=Run("SQL off");
                foreach(var rx in receivers) {rx.RXSquelchOn=true;rx.RXSquelchThreshold=(float)(-60+10*Math.Log10(rx.BufferSize));}
                double[] shut=Run("SQL above signal");
                for(int c=0;c<3;c++) Check(shut[c]<off[c]*0.001,"SQL must close "+c);
                dsp.Main.RXSquelchThreshold=(float)(-100+10*Math.Log10(2048));
                double[] first=Run("RX1 threshold below signal");
                Check(first[0]>off[0]*0.8 && first[1]<off[1]*0.001 && first[2]<off[2]*0.001,"independent thresholds");
                dsp.Rx2.RXSquelchOn=false;
                double[] second=Run("RX2 SQL disabled");
                Check(second[2]>off[2]*0.8 && second[1]<off[1]*0.001,"RX2 bypass");
                dsp.Sub.RXSquelchThreshold=dsp.Main.RXSquelchThreshold;
                double[] all=Run("all open");
                for(int c=0;c<3;c++) Check(all[c]>off[c]*0.8,"SQL reopen "+c);
                foreach(var rx in receivers) {rx.DSPMode=DSPMode.FM;rx.RXFilterLow=-8000;rx.RXFilterHigh=8000;rx.RXSquelchOn=false;}
                noise=true; double[] fmOff=Run("FM noise SQL off");
                foreach(var rx in receivers) {rx.RXSquelchOn=true;rx.FMSquelchThreshold=0.01f;}
                double[] fmShut=Run("FM noise SQL on");
                for(int c=0;c<3;c++) Check(fmOff[c]>1e-6 && fmShut[c]<fmOff[c]*0.01,"FM noise squelch "+c);
                noise=false; fmVoice=true;
                double[] fmSignal=Run("FM clean signal opens SQL");
                for(int c=0;c<3;c++) Check(fmSignal[c]>1e-5,"FM signal must open SQL "+c);
                noise=true; fmVoice=false;
                foreach(var rx in receivers) rx.RXSquelchOn=false;
                double[] fmOpen=Run("FM SQL disabled again");
                for(int c=0;c<3;c++) Check(fmOpen[c]>fmOff[c]*0.3,"FM bypass "+c);
                noise=false;
                foreach(var rx in receivers) {rx.DSPMode=DSPMode.USB;rx.RXFilterLow=150;rx.RXFilterHigh=2850;rx.RXSquelchOn=true;rx.RXSquelchThreshold=-26.887f;}
                double[] back=Run("FM to USB SQL closed");
                for(int c=0;c<3;c++) Check(back[c]<off[c]*0.001,"mode change "+c);
                Audio.SampleRate1=96000;
                double[] rate=Run("SQL persists after channel recreation");
                for(int c=0;c<3;c++) Check(rate[c]<off[c]*0.001,"recreated SQL "+c);
                Console.WriteLine("PASS: independent RX1/RX-S/RX2 amplitude/FM squelch, threshold, bypass, mode and sample-rate changes.");
                Environment.Exit(0);return 0;
            }
            catch(Exception e) {Console.WriteLine(e);Environment.Exit(1);return 1;}
        }
    }
}
