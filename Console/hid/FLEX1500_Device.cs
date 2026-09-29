//=================================================================
// FLEX1500_Device.cs
//=================================================================
// PowerSDR is a C# implementation of a Software Defined Radio.
// extracted from Flex1500.dll

#region Assembly Flex1500USB, Version=2.3.2.1, Culture=neutral, PublicKeyToken=null
// C:\Users\RADIO\source\PowerSDR_v2.8.0\Source\Console\Flex1500USB.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Runtime.InteropServices;
using Jungo.wdapi_dotnet;

namespace Jungo.flex1500_lib;

public class FLEX1500_Device
{
    private struct INTERFACE_INFO
    {
        public WDU_INTERFACE wduInterface;

        public byte bInterfaceNumber;
    }

    private const uint VALUE_NONE = uint.MaxValue;

    private IntPtr hDevice;

    private ushort wVid;

    private ushort wPid;

    private uint dwInterfaceNum;

    private uint dwAltSettingNum;

    private uint dwAddr = uint.MaxValue;

    private uint dwNumOfInterfaces;

    private uint dwNumOfAltSettingsTotal;

    private PipeList pPipesList;

    private INTERFACE_INFO[] interfacesInfo;

    internal FLEX1500_Device(IntPtr hDev, ref WDU_DEVICE pDeviceInfo)
    {
        //IL_0022: Unknown result type (might be due to invalid IL or missing references)
        //IL_0027: Unknown result type (might be due to invalid IL or missing references)
        //IL_0078: Unknown result type (might be due to invalid IL or missing references)
        //IL_007d: Unknown result type (might be due to invalid IL or missing references)
        //IL_00a8: Unknown result type (might be due to invalid IL or missing references)
        //IL_00ad: Unknown result type (might be due to invalid IL or missing references)
        //IL_00d7: Unknown result type (might be due to invalid IL or missing references)
        //IL_00dc: Unknown result type (might be due to invalid IL or missing references)
        dwNumOfInterfaces = ((WDU_CONFIGURATION)Marshal.PtrToStructure(pDeviceInfo.pActiveConfig, typeof(WDU_CONFIGURATION))).dwNumInterfaces;
        interfacesInfo = new INTERFACE_INFO[dwNumOfInterfaces];
        hDevice = hDev;
        for (uint num = 0u; num < dwNumOfInterfaces; num++)
        {
            interfacesInfo[num].wduInterface = (WDU_INTERFACE)Marshal.PtrToStructure(((WDU_DEVICE)(ref pDeviceInfo)).pActiveInterface(num), typeof(WDU_INTERFACE));
            WDU_ALTERNATE_SETTING pActiveAltSetting = (WDU_ALTERNATE_SETTING)Marshal.PtrToStructure(interfacesInfo[num].wduInterface.pActiveAltSetting, typeof(WDU_ALTERNATE_SETTING));
            if (num == 0)
            {
                dwInterfaceNum = pActiveAltSetting.Descriptor.bInterfaceNumber;
                dwAltSettingNum = pActiveAltSetting.Descriptor.bAlternateSetting;
                pPipesList = new PipeList(pDeviceInfo.Pipe0, pActiveAltSetting, hDevice);
            }

            interfacesInfo[num].bInterfaceNumber = pActiveAltSetting.Descriptor.bInterfaceNumber;
            dwNumOfAltSettingsTotal += interfacesInfo[num].wduInterface.dwNumAltSettings;
        }

        wdu_lib_decl.WDU_GetDeviceAddr(hDevice, ref dwAddr);
        wVid = pDeviceInfo.Descriptor.idVendor;
        wPid = pDeviceInfo.Descriptor.idProduct;
    }

    internal void Dispose()
    {
        pPipesList.Dispose();
    }

    public IntPtr GethDevice()
    {
        return hDevice;
    }

    internal int GetInterfaceIndexByNumber(uint dwInterfaceNumber)
    {
        for (int i = 0; i < dwNumOfInterfaces; i++)
        {
            if (interfacesInfo[i].bInterfaceNumber == dwInterfaceNumber)
            {
                return i;
            }
        }

        return -1;
    }

    public byte GetInterfaceNumberByIndex(uint index)
    {
        return interfacesInfo[index].bInterfaceNumber;
    }

    public ushort GetVid()
    {
        return wVid;
    }

    public ushort GetPid()
    {
        return wPid;
    }

    public uint GetCurrInterfaceNum()
    {
        return dwInterfaceNum;
    }

    public uint GetCurrInterfaceIndex()
    {
        return (uint)GetInterfaceIndexByNumber(dwInterfaceNum);
    }

    public uint GetCurrAlternateSettingNum()
    {
        return dwAltSettingNum;
    }

    public uint GetNumOfInteraces()
    {
        return dwNumOfInterfaces;
    }

    public uint GetNumOfAlternateSettingsPerInterface(uint dwInterfaceNumber)
    {
        int interfaceIndexByNumber = GetInterfaceIndexByNumber(dwInterfaceNumber);
        if (interfaceIndexByNumber != -1)
        {
            return interfacesInfo[interfaceIndexByNumber].wduInterface.dwNumAltSettings;
        }

        return 0u;
    }

    public uint GetNumOfAlternateSettingsTotal()
    {
        return dwNumOfAltSettingsTotal;
    }

    public PipeList GetpPipesList()
    {
        return pPipesList;
    }

    public uint ChangeAlternateSetting(uint newInterface, uint newSetting)
    {
        //IL_0053: Unknown result type (might be due to invalid IL or missing references)
        //IL_0058: Unknown result type (might be due to invalid IL or missing references)
        //IL_0084: Unknown result type (might be due to invalid IL or missing references)
        //IL_0089: Unknown result type (might be due to invalid IL or missing references)
        //IL_00b3: Unknown result type (might be due to invalid IL or missing references)
        //IL_00b8: Unknown result type (might be due to invalid IL or missing references)
        //IL_00da: Unknown result type (might be due to invalid IL or missing references)
        //IL_00df: Unknown result type (might be due to invalid IL or missing references)
        uint result = 0u;
        if (newInterface == dwInterfaceNum && newSetting == dwAltSettingNum)
        {
            return result;
        }

        result = wdu_lib_decl.WDU_SetInterface(hDevice, newInterface, newSetting);
        if (result != 0)
        {
            return result;
        }

        IntPtr intPtr = (IntPtr)0;
        result = wdu_lib_decl.WDU_GetDeviceInfo(hDevice, ref intPtr);
        if (result != 0)
        {
            return result;
        }

        WDU_DEVICE val = (WDU_DEVICE)Marshal.PtrToStructure(intPtr, typeof(WDU_DEVICE));
        int interfaceIndexByNumber = GetInterfaceIndexByNumber(newInterface);
        interfacesInfo[interfaceIndexByNumber].wduInterface = (WDU_INTERFACE)Marshal.PtrToStructure(((WDU_DEVICE)(ref val)).pActiveInterface((uint)interfaceIndexByNumber), typeof(WDU_INTERFACE));
        WDU_ALTERNATE_SETTING pActiveAltSetting = (WDU_ALTERNATE_SETTING)Marshal.PtrToStructure(interfacesInfo[interfaceIndexByNumber].wduInterface.pActiveAltSetting, typeof(WDU_ALTERNATE_SETTING));
        dwInterfaceNum = newInterface;
        dwAltSettingNum = newSetting;
        pPipesList = new PipeList(((FLEX1500_Pipe)pPipesList[0]).GetPipeInfo(), pActiveAltSetting, hDevice);
        wdu_lib_decl.WDU_PutDeviceInfo(intPtr);
        return result;
    }

    public bool IsDeviceTransferring()
    {
        for (int i = 0; i < pPipesList.Count; i++)
        {
            if (((FLEX1500_Pipe)pPipesList[i]).IsInUse())
            {
                return true;
            }
        }

        return false;
    }

    public string DeviceDescription()
    {
        string text = "Device ";
        if (dwAddr != uint.MaxValue)
        {
            _ = text + "0x" + dwAddr.ToString("X") + ", ";
        }

        return text + "vid 0x" + wVid.ToString("X") + ", pid 0x" + wPid.ToString("X") + ", ifc " + dwInterfaceNum + ", alt setting " + dwAltSettingNum + ", handle 0x" + hDevice;
    }

    public static bool operator ==(FLEX1500_Device u1, FLEX1500_Device u2)
    {
        try
        {
            return u1.hDevice == u2.hDevice;
        }
        catch
        {
            return false;
        }
    }

    public static bool operator !=(FLEX1500_Device u1, FLEX1500_Device u2)
    {
        return !(u1 == u2);
    }

    public override bool Equals(object obj)
    {
        try
        {
            return this == (FLEX1500_Device)obj;
        }
        catch
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return (int)hDevice;
    }
}
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '2.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'wdapi_dotnet1040, Version=10.4.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'wdapi_dotnet1040, Version=10.4.0.0, Culture=neutral, PublicKeyToken=null'
#endif
