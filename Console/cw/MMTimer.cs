//=================================================================
// MMTimer.cs
//=================================================================
// PowerSDR is a C# implementation of a Software Defined Radio.
// KE9NS This was extracted from FlexCW.dll


#region Assembly FlexCW, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// C:\Users\RADIO\source\PowerSDR_v2.8.0\Source\Console\FlexCW.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

//using FlexCW;
//using Multimedia;
using System; 
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PowerSDR //Multimedia;
{
    public sealed class MMTimer : IComponent, IDisposable
    {
        private delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        private delegate void EventRaiser(EventArgs e);

        private const int TIMERR_NOERROR = 0;

        private int timerID;

        private volatile TimerMode mode;

        private volatile int period;

        private volatile int resolution;

        private TimeProc timeProcPeriodic;

        private TimeProc timeProcOneShot;

        private EventRaiser tickRaiser;

        private bool running;

        private volatile bool disposed;

        private ISynchronizeInvoke synchronizingObject;

        private ISite site;

        private static TimerCaps caps;

        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                return synchronizingObject;
            }
            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                synchronizingObject = value;
            }
        }

        public int Period
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                return period;
            }
            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                if (value < Capabilities.periodMin || value > Capabilities.periodMax)
                {
                    throw new ArgumentOutOfRangeException("Period", value, "Multimedia Timer period out of range.");
                }

                period = value;
                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        public int Resolution
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                return resolution;
            }
            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Resolution", value, "Multimedia timer resolution out of range.");
                }

                resolution = value;
                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        public TimerMode Mode
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                return mode;
            }
            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                mode = value;
                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        public bool IsRunning => running;

        public static TimerCaps Capabilities => caps;

        public ISite Site
        {
            get
            {
                return site;
            }
            set
            {
                site = value;
            }
        }

        public event EventHandler Started;

        public event EventHandler Stopped;

        public event EventHandler Tick;

        public event EventHandler Disposed;

        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);

        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, int user, int mode);

        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        static MMTimer()
        {
            timeGetDevCaps(ref caps, Marshal.SizeOf((object)caps));
        }

        public MMTimer(IContainer container)
        {
            container.Add(this);
            Initialize();
        }

        public MMTimer()
        {
            Initialize();
        }

        ~MMTimer()
        {
            if (IsRunning)
            {
                timeKillEvent(timerID);
            }
        }

        private void Initialize()
        {
            mode = TimerMode.Periodic;
            period = Capabilities.periodMin;
            resolution = 1;
            running = false;
            timeProcPeriodic = TimerPeriodicEventCallback;
            timeProcOneShot = TimerOneShotEventCallback;
            tickRaiser = OnTick;
        }

        public void Start()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Timer");
            }

            if (!IsRunning)
            {
                if (Mode == TimerMode.Periodic)
                {
                    timerID = timeSetEvent(Period, Resolution, timeProcPeriodic, 0, (int)Mode);
                }
                else
                {
                    timerID = timeSetEvent(Period, Resolution, timeProcOneShot, 0, (int)Mode);
                }

                if (timerID == 0)
                {
                    throw new TimerStartException("Unable to start multimedia Timer.");
                }

                running = true;
                if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                {
                    SynchronizingObject.BeginInvoke(new EventRaiser(OnStarted), new object[1] { EventArgs.Empty });
                }
                else
                {
                    OnStarted(EventArgs.Empty);
                }
            }
        }

        public void Stop()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Timer");
            }

            if (running)
            {
                timeKillEvent(timerID);
                running = false;
                if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                {
                    SynchronizingObject.BeginInvoke(new EventRaiser(OnStopped), new object[1] { EventArgs.Empty });
                }
                else
                {
                    OnStopped(EventArgs.Empty);
                }
            }
        }

        private void TimerPeriodicEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (synchronizingObject != null)
            {
                synchronizingObject.BeginInvoke(tickRaiser, new object[1] { EventArgs.Empty });
            }
            else
            {
                OnTick(EventArgs.Empty);
            }
        }

        private void TimerOneShotEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (synchronizingObject != null)
            {
                synchronizingObject.BeginInvoke(tickRaiser, new object[1] { EventArgs.Empty });
                Stop();
            }
            else
            {
                OnTick(EventArgs.Empty);
                Stop();
            }
        }

        private void OnDisposed(EventArgs e)
        {
            this.Disposed?.Invoke(this, e);
        }

        private void OnStarted(EventArgs e)
        {
            this.Started?.Invoke(this, e);
        }

        private void OnStopped(EventArgs e)
        {
            this.Stopped?.Invoke(this, e);
        }

        private void OnTick(EventArgs e)
        {
            this.Tick?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (IsRunning)
                {
                    Stop();
                }

                disposed = true;
                OnDisposed(EventArgs.Empty);
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
