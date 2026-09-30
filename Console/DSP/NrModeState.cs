using System;
using System.Collections;
using System.Globalization;

namespace PowerSDR
{
    internal static class NrModeState
    {
        private const string Prefix = "WdspRX1NRMode/";
        internal static string Serialize(int mode)
        { return Serialize(mode, false); }

        internal static string Serialize(int mode, bool rx2)
        {
            if (mode < 0 || mode > 4) throw new ArgumentOutOfRangeException("mode");
            return (rx2 ? "WdspRX2NRMode/" : Prefix) + mode.ToString(CultureInfo.InvariantCulture);
        }

        internal static int Restore(ArrayList state, bool legacyEnabled)
        { return Restore(state, legacyEnabled, false); }

        internal static int Restore(ArrayList state, bool legacyEnabled, bool rx2)
        {
            string prefix = rx2 ? "WdspRX2NRMode/" : Prefix;
            foreach (object item in state)
            {
                string entry = item as string;
                int mode;
                if (entry != null && entry.StartsWith(prefix, StringComparison.Ordinal) &&
                    Int32.TryParse(entry.Substring(prefix.Length), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out mode) && mode >= 0 && mode <= 4)
                    return mode;
            }
            return legacyEnabled ? 1 : 0;
        }
    }
}
