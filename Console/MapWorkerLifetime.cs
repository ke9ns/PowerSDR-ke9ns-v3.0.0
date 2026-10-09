using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace PowerSDR
{
    // Register before Thread.Start, including workers not yet scheduled.
    // Never Join on the UI thread: a legacy worker may be waiting on the UI.
    internal sealed class MapWorkerLifetime
    {
        private readonly object sync = new object();
        private readonly HashSet<string> active = new HashSet<string>();
        private bool stopping;
        internal bool Stopping { get { lock (sync) return stopping; } }
        internal bool IsIdle { get { lock (sync) return active.Count == 0; } }
        internal string Pending { get { lock (sync) return String.Join(", ", active); } }
        internal void Stop() { lock (sync) stopping = true; }

        internal bool TryStart(string name, ThreadStart work, ThreadPriority priority, Action<Exception> report)
        {
            lock (sync)
            {
                if (stopping || !active.Add(name)) return false;
                try
                {
                    Thread thread = new Thread(delegate()
                    {
                        try { if (!Stopping) work(); }
                        catch (Exception ex) { try { report(ex); } catch { } }
                        finally { lock (sync) active.Remove(name); }
                    });
                    thread.Name = name;
                    thread.IsBackground = true;
                    thread.Priority = priority;
                    thread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                    thread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                    thread.Start();
                    return true;
                }
                catch { active.Remove(name); throw; }
            }
        }
    }
}
