using System;
using System.Diagnostics;
using System.IO;
namespace PowerSDR
{
    internal static unsafe class WdspStartupTests
    {
        static void Check(bool ok, string message) { if(!ok) throw new Exception(message); }
        static int Main()
        {
            try
            {
                foreach(int rate in new[]{48000,96000,192000})
                foreach(int n in new[]{64,256,2048,4096})
                {
                    WdspRxTransition gate=new WdspRxTransition();
                    gate.Block(); Check(!gate.CanProcess(n,rate),"Hardware gate");
                    gate.Release(50); int held=0;
                    while(!gate.CanProcess(n,rate)) held+=n;
                    Check(held>=rate/20 && held<rate/20+n,"Sample-based bounded settling");
                    float previous=0; float[] l=new float[n],r=new float[n];
                    for(int block=0;block<rate/100/n+2;block++)
                    {
                        for(int i=0;i<n;i++)l[i]=r[i]=1;
                        fixed(float* a=l,b=r)gate.Apply(a,b,n);
                        for(int i=0;i<n;i++)
                        { Check(l[i]>=previous && l[i]<=1 && l[i]==r[i],"Stereo monotonic ramp"); previous=l[i]; }
                    }
                    Check(previous==1,"Ramp completes");
                    gate.Block(); Check(!gate.CanProcess(n,rate),"Repeated PTT edge");
                    gate.Release(0); Check(gate.CanProcess(n,rate),"No relay hold for other radios");
                }
                Console.WriteLine("PASS: transition gate/ramp at 48/96/192 kHz, buffers 64/256/2048/4096.");
                Audio.SampleRate1=192000;
                DSP dsp=new DSP(); dsp.Main.Active=dsp.Sub.Active=dsp.Rx2.Active=true;
                DspBackend.Register(dsp);
                string directory=Path.Combine(Environment.CurrentDirectory,"cache-unicode-\u00e1");
                string file=Path.Combine(directory,"wdsp-fftw-x86.wisdom");
                bool cached=File.Exists(file); Stopwatch timer=Stopwatch.StartNew();
                DspBackend.PrepareForAudio(2048,192000,directory);
                Console.WriteLine("Three RX channels prepared: cached={0}, time={1} ms",cached,timer.ElapsedMilliseconds);
                Check(File.Exists(file) && new FileInfo(file).Length>100,"Wisdom cache export");
                Check(WdspNative.fftw_import_wisdom_from_string(File.ReadAllText(file))!=0,"Wisdom cache import");
                Console.WriteLine("PASS: native wisdom round trip including Unicode profile path.");
                float[] input=new float[2048], zero=new float[2048];
                float[][] output=new float[6][];
                for(int c=0;c<6;c++)output[c]=new float[2048];
                DspBackend.BeginRadioTransition(true);
                fixed(float* a=input,b=zero,o0=output[0],o1=output[1],o2=output[2],o3=output[3],o4=output[4],o5=output[5])
                {
                    float** ins=stackalloc float*[6]; float** outs=stackalloc float*[6];
                    for(int c=0;c<6;c++)ins[c]=(c%2==0)?a:b;
                    outs[0]=o0;outs[1]=o1;outs[2]=o2;outs[3]=o3;outs[4]=o4;outs[5]=o5;
                    for(int i=0;i<2048;i++)input[i]=0.75f;
                    DspBackend.ProcessMultiChannel(ins,outs,2048,true);
                    for(int i=0;i<2048;i++)
                        Check(o0[i]==0 && o1[i]==0 && o4[i]==0 && o5[i]==0 && o2[i]==-19 && o3[i]==-20,"RX-only transition isolation");
                    // Changing the RX2 monitor option during TX must not leave
                    // the RX2 gate latched shut on the following receive cycle.
                    DspBackend.BeginRadioTransition(false);
                    DspBackend.CompleteReceiveTransition(0);
                    Audio.RX2AutoMuteTX=false;
                    DspBackend.BeginRadioTransition(false);
                    double energy=0;
                    for(int block=0;block<120;block++)
                    {
                        for(int i=0;i<2048;i++)input[i]=(float)(1e-4*Math.Sin(2*Math.PI*1000*(block*2048+i)/192000.0));
                        DspBackend.ProcessMultiChannel(ins,outs,2048,false);
                        Check(o0[0]==-17 && o2[0]==-19 && o3[0]==-20,"TX preserved while RX2 monitors");
                        Check(o4[0]!=-21,"RX2 WDSP fallback during monitor");
                        if(block>80)for(int i=0;i<2048;i++)energy+=o4[i]*o4[i];
                        System.Threading.Thread.Sleep(11);
                    }
                    Check(energy>1e-9,"RX2 monitor must remain audible");
                }
                Console.WriteLine("PASS: RX1/RX-S/RX2 transition mute, TX pair isolation and uninterrupted RX2 monitor.");
                Environment.Exit(0); return 0;
            }
            catch(Exception ex) { Console.WriteLine(ex); Environment.Exit(1); return 1; }
        }
    }
}
