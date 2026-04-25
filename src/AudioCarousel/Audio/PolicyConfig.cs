using System.Runtime.InteropServices;

namespace AudioCarousel.Audio;

// IPolicyConfig is undocumented but stable from Windows Vista through Windows 11.
// CLSID and IID values are well-known.
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    [PreserveSig] int GetMixFormat();
    [PreserveSig] int GetDeviceFormat();
    [PreserveSig] int ResetDeviceFormat();
    [PreserveSig] int SetDeviceFormat();
    [PreserveSig] int GetProcessingPeriod();
    [PreserveSig] int SetProcessingPeriod();
    [PreserveSig] int GetShareMode();
    [PreserveSig] int SetShareMode();
    [PreserveSig] int GetPropertyValue();
    [PreserveSig] int SetPropertyValue();

    [PreserveSig]
    int SetDefaultEndpoint(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        uint role);

    [PreserveSig] int SetEndpointVisibility();
}

[ComImport]
[Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
internal class PolicyConfigClient
{
}

internal static class PolicyConfig
{
    public static void SetDefaultEndpoint(string deviceId, uint role)
    {
        var client = (IPolicyConfig)new PolicyConfigClient();
        try
        {
            int hr = client.SetDefaultEndpoint(deviceId, role);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);
        }
        finally
        {
            Marshal.ReleaseComObject(client);
        }
    }
}
