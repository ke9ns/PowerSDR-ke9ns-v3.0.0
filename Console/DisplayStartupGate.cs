using System;

namespace PowerSDR
{
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
