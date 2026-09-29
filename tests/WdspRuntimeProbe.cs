using System;
using System.Runtime.InteropServices;

internal static class WdspRuntimeProbe
{
    [DllImport("wdsp.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetWDSPVersion();

    private static int Main()
    {
        try
        {
            Console.WriteLine("WDSP_VERSION=" + GetWDSPVersion());
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }
}
