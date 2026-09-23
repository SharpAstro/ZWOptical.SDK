using TianWen.DAL;
using static ZWOptical.SDK.ASICamera2;
using static ZWOptical.SDK.ASICamera2.ASI_ERROR_CODE;
using static ZWOptical.SDK.EAFFocuser1_6;
using static ZWOptical.SDK.EAFFocuser1_6.EAF_ERROR_CODE;
using static ZWOptical.SDK.EFW1_7;
using static ZWOptical.SDK.EFW1_7.EFW_ERROR_CODE;

namespace ZWOptical.SDK;

public class DeviceIterator<TDeviceInfo> : NativeDeviceIteratorBase<TDeviceInfo>
    where TDeviceInfo : struct, INativeDeviceInfo
{
    protected override int DeviceCount()
    {
        if (typeof(TDeviceInfo) == typeof(ASI_CAMERA_INFO))
            return ASIGetNumOfConnectedCameras();
        else if (typeof(TDeviceInfo) == typeof(EAF_INFO))
            return EAFGetNum();
        else if (typeof(TDeviceInfo) == typeof(EFW_INFO))
            return EFWGetNum();

        return 0;
    }

    protected override TDeviceInfo? GetDeviceInfo(int index)
    {
        if (typeof(TDeviceInfo) == typeof(ASI_CAMERA_INFO))
        {
            if (ASIGetCameraProperty(out var camInfo, index) is ASI_SUCCESS)
                return (TDeviceInfo)(INativeDeviceInfo)camInfo;
        }
        else if (typeof(TDeviceInfo) == typeof(EAF_INFO))
        {
            if (EAFGetID(index, out var eafId) is EAF_SUCCESS
                && EAFGetProperty(eafId, out var eafInfo) is EAF_SUCCESS && eafInfo.ID == eafId)
                return (TDeviceInfo)(INativeDeviceInfo)eafInfo;
        }
        else if (typeof(TDeviceInfo) == typeof(EFW_INFO))
        {
            if (EFWGetID(index, out var efwId) is EFW_SUCCESS
                && TryGetEfwProperty(efwId, out var efwInfo) && efwInfo.ID == efwId)
                return (TDeviceInfo)(INativeDeviceInfo)efwInfo;
        }

        return null;
    }

    /// <summary>
    /// <c>EFWGetProperty</c>, opening the wheel for the read when the SDK insists on it.
    /// </summary>
    /// <remarks>
    /// <para>The header's usage notes list <c>EFWGetProperty</c> BEFORE <c>EFWOpen</c>, and this
    /// iterator followed them, but EFW SDK 1.7 answers <c>EFW_ERROR_CLOSED</c> for a wheel that is not
    /// open, with the name filled in and <c>slotNum = 0</c>. Requiring success therefore dropped EVERY
    /// wheel, silently: <c>EFWGetNum</c> said 1 and the iterator yielded nothing. Measured on an
    /// EFW(8PosPlan) 2026-09-23: 9 (<c>EFW_ERROR_CLOSED</c>) and 0 slots before the open, success and 8
    /// slots after it.</para>
    /// <para>Opened and closed again here rather than accepted half-read, because the slot count is
    /// part of what a consumer is told (tianwen seeds its filter names from it), and a struct copied
    /// with 0 would carry that 0 even after the consumer opened the wheel itself.</para>
    /// </remarks>
    private static bool TryGetEfwProperty(int efwId, out EFW_INFO efwInfo)
    {
        var error = EFWGetProperty(efwId, out efwInfo);
        if (error is EFW_ERROR_CLOSED && EFWOpen(efwId) is EFW_SUCCESS)
        {
            try
            {
                error = EFWGetProperty(efwId, out efwInfo);
            }
            finally
            {
                _ = EFWClose(efwId);
            }
        }

        return error is EFW_SUCCESS;
    }
}
