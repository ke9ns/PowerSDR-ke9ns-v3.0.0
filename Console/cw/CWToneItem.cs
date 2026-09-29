//=================================================================
// CWToneItme.cs
//=================================================================
// PowerSDR is a C# implementation of a Software Defined Radio.
// KE9NS This was extracted from FlexCW.dll


#region Assembly FlexCW, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// C:\Users\RADIO\source\PowerSDR_v2.8.0\Source\Console\FlexCW.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion


//using FlexCW;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PowerSDR //FlexCW;
{
    public class CWToneItem
    {
        private bool state;

        private double time;

        public bool State => state;

        public double Time => time;

        public CWToneItem(bool _state, double _time)
        {
            state = _state;
            time = _time;
        }

        public override string ToString()
        {
            return time.ToString("f1") + ": " + state;
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
