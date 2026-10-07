using System;

namespace PowerSDR
{
    // A frame count is not a timeout: five equal frames can be normal while
    // native channels start, or while the display outruns the FFT producer.
    internal sealed class DspRestartWatchdog
    {
        private readonly object sync = new object();
        private readonly double[] values = new double[6];
        private readonly long[] changed = new long[6], observed = new long[6];
        private readonly bool[] seen = new bool[6];
        private long start, baseline, lastBlocks, progressStart = -1, lastProgress;
        private bool armed;

        internal void Reset(long now, long blocks)
        {
            lock (sync)
            {
                start = lastProgress = now;
                baseline = lastBlocks = blocks;
                progressStart = -1;
                armed = false;
                Array.Clear(seen, 0, seen.Length);
            }
        }

        internal bool Observe(int source, double spectrumSum, long now, long blocks)
        {
            lock (sync)
            {
                if (blocks != lastBlocks)
                {
                    if (progressStart < 0 || now - lastProgress > 1000)
                    { progressStart = now; baseline = lastBlocks; }
                    lastProgress = now;
                    lastBlocks = blocks;
                }
                if (!armed)
                {
                    // Permit bounded cold initialization, but still detect a
                    // radio which never delivers even its first audio block.
                    bool ready = progressStart >= 0 && now - lastProgress <= 1000 &&
                        now - progressStart >= 2000 && blocks - baseline >= 16;
                    if (!ready && now - start < 15000) return false;
                    armed = true;
                }
                if (!seen[source] || now - observed[source] > 1500 ||
                    spectrumSum != values[source] || Double.IsNaN(spectrumSum))
                {
                    seen[source] = true;
                    changed[source] = now;
                    values[source] = spectrumSum;
                }
                observed[source] = now;
                if (now - changed[source] < 2000) return false;
                Reset(now, blocks); // One restart request, shared by all views.
                return true;
            }
        }
    }

    // Display-only startup mitigation. Never changes the user's stored FPS.
    internal sealed class DisplayStartupGate
    {
        private long lastBlocks, baselineBlocks, firstProgress = -1, lastProgress;
        private bool released;

        internal DisplayStartupGate(long completedBlocks) { lastBlocks = completedBlocks; }

        internal int GetFps(int requested, long milliseconds, long completedBlocks)
        {
            requested = Math.Max(1, requested);
            if (released) return requested;
            if (completedBlocks != lastBlocks)
            {
                if (firstProgress < 0 || milliseconds - lastProgress > 500)
                {
                    firstProgress = milliseconds;
                    baselineBlocks = lastBlocks;
                }
                lastProgress = milliseconds;
                lastBlocks = completedBlocks;
            }
            if (milliseconds - lastProgress > 500) firstProgress = -1;
            if (firstProgress < 0 || completedBlocks - baselineBlocks < 8 ||
                milliseconds - firstProgress < 3000)
                return Math.Min(requested, 25);

            long rampSteps = (milliseconds - firstProgress - 3000) / 500;
            int ceiling = (int)Math.Min(Int32.MaxValue, 25L + rampSteps * 5);
            if (requested > 25 && ceiling >= requested) released = true;
            return Math.Min(requested, ceiling);
        }
    }
}
