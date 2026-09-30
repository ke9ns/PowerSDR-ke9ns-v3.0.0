using System;
using System.IO;

namespace PowerSDR
{
    // Keep the profile stable across maintenance builds and separate from v2.
    internal static class ProfilePaths
    {
        internal const string ProfileName = "PowerSDR v3.0.0";
        internal static string DefaultDirectory
        {
            get
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FlexRadio Systems", ProfileName);
#if DEBUG
                path = Path.Combine(path, "Debug");
#endif
                return Normalize(path);
            }
        }

        private static string dataDirectory = DefaultDirectory;
        internal static string DataDirectory
        {
            get { return dataDirectory; }
            set { dataDirectory = Normalize(value); }
        }

        internal static string WisdomFile
        {
            get { return Path.Combine(DataDirectory, "wisdom"); }
        }

        private static string Normalize(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A profile directory is required.", "path");
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }
    }
}
