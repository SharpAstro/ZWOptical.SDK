using TianWen.DAL;
using static ZWOptical.SDK.ASICamera2;
using static ZWOptical.SDK.ASICamera2.ASI_ERROR_CODE;
using static ZWOptical.SDK.EAFFocuser1_6;
using static ZWOptical.SDK.EAFFocuser1_6.EAF_ERROR_CODE;
using static ZWOptical.SDK.EFW1_7;
using static ZWOptical.SDK.EFW1_7.EFW_ERROR_CODE;

namespace ZWOptical.SDK
{
    public class DeviceIterator<TDeviceInfo> : NativeDeviceIteratorBase<TDeviceInfo>
        where TDeviceInfo : struct, INativeDeviceInfo
    {
        protected override int DeviceCount()
        {
            if (typeof(TDeviceInfo) == typeof(ASI_CAMERA_INFO))
            {
                return ASIGetNumOfConnectedCameras();
            }
            else if (typeof(TDeviceInfo) == typeof(EAF_INFO))
            {
                return EAFGetNum();
            }
            else if (typeof(TDeviceInfo) == typeof(EFW_INFO))
            {
                return EFWGetNum();
            }

            return 0;
        }

        protected override (int? DeviceId, TDeviceInfo? DeviceInfo) GetId(int index)
        {
            if (typeof(TDeviceInfo) == typeof(ASI_CAMERA_INFO))
            {
                if (ASIGetCameraProperty(out var camInfo, index) is ASI_SUCCESS)
                {
                    return (camInfo.CameraID, (TDeviceInfo)(INativeDeviceInfo)camInfo);
                }
            }
            else if (typeof(TDeviceInfo) == typeof(EAF_INFO))
            {
                if (EAFGetID(index, out var eafId) is EAF_SUCCESS
                    && EAFGetProperty(eafId, out var eafInfo) is EAF_SUCCESS && eafInfo.ID == eafId)
                {
                    return (eafInfo.ID,  (TDeviceInfo)(INativeDeviceInfo)eafInfo);
                }
            }
            else if (typeof(TDeviceInfo) == typeof(EFW_INFO))
            {
                if (EFWGetID(index, out var efwId) is EFW_SUCCESS
                    && EFWGetProperty(efwId, out var efwInfo) is EFW_SUCCESS && efwInfo.ID == efwId)
                {
                    return (efwInfo.ID, (TDeviceInfo)(INativeDeviceInfo)efwInfo);
                }
            }

            return (null, null);
        }
    }
}
