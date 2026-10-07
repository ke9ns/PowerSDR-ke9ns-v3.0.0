using System;
namespace PowerSDR
{
    internal static class DisplayStartupGateTests
    {
        static void Check(bool ok) { if (!ok) throw new Exception("Startup pacing regression"); }
        static int Main()
        {
            foreach (int fps in new[] { 10, 25, 31, 32, 50, 120 })
            {
                var watchdog = new DspRestartWatchdog();
                watchdog.Reset(0, 100);
                int step = Math.Max(1, 1000 / fps);
                for (int t = 0; t < 8000; t += step)
                    Check(!watchdog.Observe(0, 0, t, 100)); // Cold native init.
                for (int t = 8000; t < 12000; t += step)
                    Check(!watchdog.Observe(0, t, t, 100 + (t - 8000) / 10));
                long frozenAt = -1;
                for (int t = 12000; t < 14500; t += step)
                    if (watchdog.Observe(0, 123, t, 600 + (t - 12000) / 10))
                    { frozenAt = t; break; }
                Check(frozenAt >= 14000 && frozenAt <= 14000 + step);
                Check(!watchdog.Observe(1, 123, frozenAt, 1000)); // No duplicate.
                watchdog.Reset(15000, 1000); // PTT edge / power cycle.
                Check(!watchdog.Observe(0, 123, 15001, 1000));
            }
            var dead = new DspRestartWatchdog();
            dead.Reset(0, 0);
            for (int t = 0; t < 17000; t += 100)
                Check(!dead.Observe(0, 0, t, 0));
            Check(dead.Observe(0, 0, 17000, 0)); // Grace is not permanent.
            var quiet = new DspRestartWatchdog();
            quiet.Reset(0, 0);
            for (int t = 0; t < 10000; t += 100)
            {
                // Sub-integer changes are real spectrum progress, not a stall.
                Check(!quiet.Observe(0, 123 + t * 0.00001f, t, t / 10));
                Check(!quiet.Observe(1, 456 + t * 0.00001f, t, t / 10));
            }
            System.Console.WriteLine("PASS: watchdog startup, real stall, no-audio timeout, PTT reset, independent views and FPS invariance.");
            var gate = new DisplayStartupGate(100);
            Check(gate.GetFps(50, 0, 100) == 25);
            Check(gate.GetFps(50, 60000, 100) == 25); // Time alone must not release.
            for (int t = 0; t <= 6000; t += 100)
            {
                int fps = gate.GetFps(50, 60000 + t, 101 + t / 10);
                Check(fps == Math.Min(50, t < 3000 ? 25 : 25 + ((t - 3000) / 500) * 5));
            }
            Check(gate.GetFps(60, 70000, 800) == 60); // User can raise after startup.
            var restarted = new DisplayStartupGate(800);
            Check(restarted.GetFps(50, 0, 800) == 25);
            Check(restarted.GetFps(15, 100, 801) == 15);
            Check(restarted.GetFps(50, 1000, 802) == 25); // Long processing gap restarts warmup.
            Check(restarted.GetFps(50, 5000, 802) == 25); // No new audio progress.
            foreach (int target in new[] { 25, 31, 32, 50 })
            {
                var fresh = new DisplayStartupGate(0);
                int fps = 0;
                for (int t = 0; t <= 7000; t += 100)
                {
                    fps = fresh.GetFps(target, t, t / 10 + 1);
                    Check(fps <= target && (t >= 3000 || fps <= 25));
                }
                Check(fps == target);
            }
            System.Console.WriteLine("PASS: cold start, stalled audio, restart, low FPS and 25/31/32/50 FPS targets.");
            return 0;
        }
    }
}
