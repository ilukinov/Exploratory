using System.Management;
using System.Runtime.InteropServices;

namespace HPAICOmpanionTester.Support;

/// <summary>
/// Reads system hardware state (volume, brightness) using Windows APIs.
/// Used to verify that the Perform agent's device control commands
/// actually changed the system, not just returned a chat response.
/// </summary>
public static class SystemStateReader
{
    // ── Volume (Core Audio API via COM) ──────────────────────────

    /// <summary>
    /// Returns the current master volume as a percentage (0-100).
    /// Uses the Windows Core Audio IAudioEndpointVolume interface.
    /// </summary>
    public static int GetVolume()
    {
        return WithEndpointVolume(epv =>
        {
            epv.GetMasterVolumeLevelScalar(out var level);
            return (int)Math.Round(level * 100);
        });
    }

    /// <summary>
    /// Returns true if the master audio output is muted.
    /// </summary>
    public static bool IsMuted()
    {
        return WithEndpointVolume(epv =>
        {
            epv.GetMute(out var mute);
            return mute;
        });
    }

    /// <summary>
    /// Acquires the default audio endpoint, executes <paramref name="action"/>,
    /// and releases all COM objects regardless of outcome.
    /// </summary>
    private static T WithEndpointVolume<T>(Func<IAudioEndpointVolume, T> action)
    {
        object? enumeratorObj = null;
        object? deviceObj = null;
        object? volumeObj = null;
        try
        {
            enumeratorObj = new MMDeviceEnumerator();
            var enumerator = (IMMDeviceEnumerator)enumeratorObj;
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
            deviceObj = device;

            var iid = typeof(IAudioEndpointVolume).GUID;
            device.Activate(ref iid, 0, IntPtr.Zero, out volumeObj);
            return action((IAudioEndpointVolume)volumeObj);
        }
        finally
        {
            if (volumeObj is not null) Marshal.ReleaseComObject(volumeObj);
            if (deviceObj is not null) Marshal.ReleaseComObject(deviceObj);
            if (enumeratorObj is not null) Marshal.ReleaseComObject(enumeratorObj);
        }
    }

    // ── Brightness (WMI) ─────────────────────────────────────────

    /// <summary>
    /// Returns the current screen brightness as a percentage (0-100).
    /// Uses WMI WmiMonitorBrightness class. Returns -1 if unavailable
    /// (e.g. on a desktop with no built-in display).
    /// </summary>
    public static int GetBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentBrightness FROM WmiMonitorBrightness");

            foreach (var obj in searcher.Get())
                return Convert.ToInt32(obj["CurrentBrightness"]);

            return -1;
        }
        catch
        {
            return -1;
        }
    }

    // ── COM interop for Core Audio API ───────────────────────────

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    private enum EDataFlow { eRender = 0 }
    private enum ERole { eMultimedia = 1 }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out int pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(int nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(int nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(int nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(int nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        int GetVolumeStepInfo(out int pnStep, out int pnStepCount);
        int VolumeStepUp(ref Guid pguidEventContext);
        int VolumeStepDown(ref Guid pguidEventContext);
        int QueryHardwareSupport(out int pdwHardwareSupportMask);
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }
}
