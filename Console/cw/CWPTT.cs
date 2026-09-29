//=================================================================
// CWPTT.cs
//=================================================================
// PowerSDR is a C# implementation of a Software Defined Radio.
// KE9NS This was extracted from FlexCW.dll

#region Assembly FlexCW, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// C:\Users\RADIO\source\PowerSDR_v2.8.0\Source\Console\FlexCW.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion


//using FlexCW;
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using Multimedia;

namespace PowerSDR //FlexCW;
{
    public class CWPTT
    {
        public delegate void MoxCallback(bool val);

        public delegate void MuteCallback(bool val);

        private static MMTimer timer = new MMTimer();

        public static event MoxCallback MoxChanged;

        public static event MuteCallback MuteChanged;

        private static void OnMoxChanged(bool b)
        {
            Debug.WriteLine("CWPTT.OnMoxChanged: " + b);

            CWPTT.MoxChanged?.Invoke(b);
        }

        private static void OnMuteChanged(bool b)
        {
            CWPTT.MuteChanged?.Invoke(b);
        }

        public static void Init()
        {
            Debug.WriteLine("CWPTT.Init");

            timer.Mode = TimerMode.Periodic;
            timer.Period = 1;
            timer.Resolution = 0;
            timer.Tick += timer_Tick;
        }

        public static void Start() //ke9ns: console calls this after calling CWKeyer.Reset() from  chkPower_CheckedChanged.
        {
            Debug.WriteLine("CWPTT.Start");

            timer.Start();
        }

        public static void Stop()
        {
            Debug.WriteLine("CWPTT.Stop");

            timer.Stop();
        }

        private static void timer_Tick(object sender, EventArgs e) // ke9ns: after start (above), this is called every 1 ms, and it checks the CWKeyer PTT and Mute queues to see if any items are due to be processed. 
        {

         // Debug.WriteLine("CWPTT.timer_Tick"); //ke9ns: cannot use this as it stalls operation due to 1ms timer.

            double currentTime = CWSensorItem.GetCurrentTime();
            CWKeyer.Advance(currentTime);

            if (CWKeyer.PTTQueueCount() > 0)
            {

                Debug.WriteLine("CWPTT.timer_Tick: PTTQueueCount=" + CWKeyer.PTTQueueCount());

                CWPTTItem cWPTTItem = CWKeyer.PTTQueuePeek();
                if (currentTime > cWPTTItem.Time)
                {
                    CWKeyer.PTTDequeue();
                    if (!cWPTTItem.Ignore)
                    {
                        Debug.WriteLine("CWPTT.timer_Tick: PTTDequeue: " + cWPTTItem);

                        OnMoxChanged(cWPTTItem.State);
                    }
                }
            }

            if (CWKeyer.MuteQueueCount() > 0)
            {
                Debug.WriteLine("CWPTT.timer_Tick: MuteQueueCount=" + CWKeyer.MuteQueueCount());

                CWMuteItem cWMuteItem = CWKeyer.MuteQueuePeek();
                if (currentTime > cWMuteItem.Time)
                {
                    CWKeyer.MuteDequeue();
                    OnMuteChanged(cWMuteItem.MOX);
                }
            }
        }
    }
}
#if false // Decompilation log
'170' items in cache
------------------
Resolve: 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '2.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '2.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll'
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
#endif
