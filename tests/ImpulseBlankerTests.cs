using System;
namespace PowerSDR
{
    internal unsafe class ImpulseBlankerTests
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static double Run(int block, int rate, bool nb1, bool nb2, int mode, double threshold)
        {
            using (WdspImpulseBlankers blanker = new WdspImpulseBlankers(4))
            {
                float[] l = new float[block], r = new float[block];
                WdspBlankerSettings s = new WdspBlankerSettings(threshold, .0001, .0001, .0001, .05, mode);
                double peak = 0;
                int warmup = (int)Math.Ceiling(rate * 1.0 / block);
                fixed (float* il = l, ir = r)
                for (int b = 0; b < warmup + 32; b++)
                {
                    for (int i = 0; i < block; i++) { l[i] = .01f; r[i] = .005f; }
                    if (b == warmup) { l[block/2] = 10; r[block/2] = 5; }
                    bool processed = blanker.Process(il, ir, block, rate, nb1, nb2, s, s);
                    Check(processed == (nb1 || nb2), "enable/bypass routing");
                    Check(l[block/2] == (b == warmup ? 10 : .01f), "input IQ was overwritten");
                    if (b >= warmup)
                        for (int i = 0; i < block; i++)
                        {
                            float value = processed ? blanker.Left[i] : l[i];
                            Check(!Single.IsNaN(value) && !Single.IsInfinity(value), "non-finite output");
                            peak = Math.Max(peak, Math.Abs(value));
                        }
                }
                Console.WriteLine("block={0} rate={1} NB1={2} NB2={3} mode={4} threshold={5}: peak={6:G6}",
                    block, rate, nb1, nb2, mode, threshold, peak);
                return peak;
            }
        }
        static void Main()
        {
            try
            {
                Check(Run(2048,48000,false,false,0,30) == 10, "bypass must retain impulse");
                Check(Run(2048,48000,true,false,0,30) < .02, "NB1 must suppress impulse");
                for(int mode=0;mode<5;mode++)
                    Check(Run(2048,48000,false,true,mode,30) < .02, "NB2 mode must suppress impulse");
                Check(Run(2048,48000,true,false,0,10000) > 1, "NB1 threshold must affect detection");
                Check(Run(2048,48000,false,true,0,10000) > 1, "NB2 threshold must affect detection");
                Check(Run(4096,96000,true,true,4,30) < .02, "both at 96k");
                Check(Run(8192,192000,true,true,4,30) < .02, "large callback at 192k");
                using(WdspImpulseBlankers b=new WdspImpulseBlankers(4))
                {
                    float[] a=new float[8192]; WdspBlankerSettings s=WdspBlankerSettings.Default();
                    fixed(float* p=a)
                    {
                        b.Process(p,p,512,48000,true,true,s,s);
                        b.Process(p,p,512,48000,false,false,s,s);
                        s=new WdspBlankerSettings(25,.002,.002,.002,.1,4);
                        b.Process(p,p,4096,96000,true,true,s,s);
                        b.Process(p,p,8192,192000,true,true,s,s);
                        b.Process(p,p,512,48000,true,true,s,s);
                    }
                }
                Console.WriteLine("PASS: both real blankers, all NB2 modes, threshold, bypass, input preservation, resize/rate/re-enable.");
                Environment.Exit(0);
            }
            catch(Exception ex) { Console.WriteLine(ex); Environment.Exit(1); }
        }
    }
}
