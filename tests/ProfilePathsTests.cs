using System;
using System.IO;

namespace PowerSDR
{
    internal static class ProfilePathsTests
    {
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Main()
        {
            string expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlexRadio Systems", "PowerSDR v3.0.0");
#if DEBUG
            expected = Path.Combine(expected, "Debug");
#endif
            Check(ProfilePaths.DefaultDirectory == expected + Path.DirectorySeparatorChar,
                "Default profile/version mismatch");
            Check(ProfilePaths.DataDirectory == ProfilePaths.DefaultDirectory,
                "Initial profile mismatch");
            Check(ProfilePaths.WisdomFile == Path.Combine(expected, "wisdom"),
                "Default wisdom mismatch");
            string custom = Path.Combine(Path.GetTempPath(), "PowerSDR profile path test");
            ProfilePaths.DataDirectory = custom;
            Check(ProfilePaths.DataDirectory == custom + Path.DirectorySeparatorChar,
                "Custom path normalization failed");
            Check(ProfilePaths.WisdomFile == Path.Combine(custom, "wisdom"),
                "DSP must use the explicit custom profile, without a Debug suffix");
            ProfilePaths.DataDirectory = custom + Path.DirectorySeparatorChar;
            Check(ProfilePaths.DataDirectory == custom + Path.DirectorySeparatorChar,
                "Duplicate path separator");
            try
            {
                ProfilePaths.DataDirectory = " ";
                throw new Exception("Empty path accepted");
            }
            catch (ArgumentException) { }
            Check(ProfilePaths.WisdomFile == Path.Combine(custom, "wisdom"),
                "Invalid path modified the active profile");
            ProfilePaths.DataDirectory = ProfilePaths.DefaultDirectory;
            System.Console.WriteLine("PASS: v3 default/custom profile and wisdom paths (no user files written).");
        }
    }
}
