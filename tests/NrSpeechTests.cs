using System;
using System.IO;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.Threading;
namespace PowerSDR
{
    internal unsafe class NrSpeechTests
    {
        static float[] speech;
        static DSP dsp = new DSP();
        static bool physicalRx2;
        static double Run(int rate, int nr)
        {
            Audio.SampleRate1 = rate;
            DspBackend.SetRx1NoiseReductionMode(nr);
            DspBackend.SetRx2NoiseReductionMode(nr);
            const int n = 2048;
            float[] l=new float[n], r=new float[n], ol=new float[n], orr=new float[n];
            float[] other0=new float[n], other1=new float[n], tx0=new float[n], tx1=new float[n];
            double energy=0; long samples=0; int fallback=0;
            int blocks=rate*8/n;
            for(int b=0;b<blocks;b++)
            {
                for(int i=0;i<n;i++)
                {
                    double position=((long)b*n+i)*48000.0/rate;
                    int j=(int)position % (speech.Length-1);
                    l[i]=(float)(speech[j]+(speech[j+1]-speech[j])*(position-Math.Floor(position)));
                }
                fixed(float* il=l, ir=r, o=ol, p=orr, a=other0, c=other1, t=tx0, u=tx1)
                {
                    if (!physicalRx2) DspBackend.ProcessStereo(il,ir,o,p,n,true);
                    else
                    {
                        float** ins=stackalloc float*[6]; float** outs=stackalloc float*[6];
                        ins[0]=il; ins[1]=ir; ins[2]=ir; ins[3]=ir; ins[4]=il; ins[5]=ir;
                        outs[0]=a; outs[1]=c; outs[2]=t; outs[3]=u; outs[4]=o; outs[5]=p;
                        DspBackend.ProcessMultiChannel(ins,outs,n,true);
                    }
                }
                if(b*n>rate*2)
                {
                    if(ol[0]==(physicalRx2 ? -21 : -17)) fallback++;
                    for(int i=0;i<n;i++)
                    {
                        if(float.IsNaN(ol[i]) || float.IsInfinity(ol[i])) throw new Exception("Nonfinite output");
                        energy+=ol[i]*ol[i]; samples++;
                    }
                }
                Thread.Sleep((int)Math.Ceiling(1000.0*n/rate));
            }
            double rms=Math.Sqrt(energy/samples);
            Console.WriteLine("rate={0} NR={1} speech RMS={2:G6} fallback={3}",rate,nr,rms,fallback);
            if(fallback!=0 || rms<1e-5) throw new Exception("Silent or fallback speech");
            return rms;
        }
        static int Main(string[] args)
        {
            try
            {
                physicalRx2 = args.Length > 0 && args[0] == "--rx2";
                Environment.SetEnvironmentVariable("POWERSDR_WDSP_RX1",null);
                using(var stream=new MemoryStream())
                using(var synth=new SpeechSynthesizer())
                {
                    synth.SetOutputToAudioStream(stream,new SpeechAudioFormatInfo(48000,AudioBitsPerSample.Sixteen,AudioChannel.Mono));
                    synth.Speak("This is a radio receiver test. One two three four five. The noise reduction must preserve speech. Testing the receiver audio with a strong and clear voice.");
                    byte[] raw=stream.ToArray(); speech=new float[raw.Length/2];
                    for(int i=0;i<speech.Length;i++) speech[i]=BitConverter.ToInt16(raw,2*i)/32768f * 0.5f;
                }
                dsp.Main.Active=true; dsp.Main.DSPMode=DSPMode.DSB;
                dsp.Main.RXFilterLow=-3000; dsp.Main.RXFilterHigh=3000;
                dsp.Rx2.Active=physicalRx2; dsp.Rx2.DSPMode=DSPMode.DSB;
                dsp.Rx2.RXFilterLow=-3000; dsp.Rx2.RXFilterHigh=3000;
                DspBackend.Register(dsp);
                foreach(int rate in new[]{48000,96000,192000})
                {
                    double baseline=Run(rate,0), processed=Run(rate,3);
                    double ratio=processed/baseline;
                    Console.WriteLine("NR3 speech level relative to off: {0:F2} dB",20*Math.Log10(ratio));
                    if(ratio<0.03 || ratio>2) throw new Exception("NR3 voice excessively suppressed/amplified");
                }
                Console.WriteLine("PASS: synthesized speech survives NR3 at 48/96/192 kHz; no native fallback.");
                return 0;
            }
            catch(Exception ex) { Console.WriteLine(ex); return 1; }
        }
    }
}
