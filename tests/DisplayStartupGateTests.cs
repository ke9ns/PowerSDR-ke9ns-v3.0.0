using System;
namespace PowerSDR
{
    internal static class DisplayStartupGateTests
    {
        static void Check(bool ok) { if (!ok) throw new Exception("Startup pacing regression"); }
        static int Main()
        {
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
