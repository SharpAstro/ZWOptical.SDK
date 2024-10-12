using System.Collections;
using System.Collections.Generic;
using static ZWOptical.SDK.ASICamera2;
using static ZWOptical.SDK.ASICamera2.ASI_ERROR_CODE;
using static ZWOptical.SDK.EAFFocuser1_6;
using static ZWOptical.SDK.EAFFocuser1_6.EAF_ERROR_CODE;
using static ZWOptical.SDK.EFW1_7;
using static ZWOptical.SDK.EFW1_7.EFW_ERROR_CODE;

namespace ZWOptical.SDK
{
    public class DeviceIterator<TDeviceInfo> : IEnumerable<int>
        where TDeviceInfo : struct, IZWODeviceInfo
    {
        public IEnumerator<int> GetEnumerator()
        {
            var count = DeviceCount();

            for (var index = 0; index < count; index++)
            {
                var id = GetId(index);
                if (id.HasValue)
                {
                    yield return id.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int DeviceCount()
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

        int? GetId(int index)
        {
            if (typeof(TDeviceInfo) == typeof(ASI_CAMERA_INFO))
            {
                return ASIGetCameraProperty(out var camInfo, index) is ASI_SUCCESS ? camInfo.CameraID : null as int?;
            }
            else if (typeof(TDeviceInfo) == typeof(EAF_INFO))
            {
                return EAFGetID(index, out var eafId) is EAF_SUCCESS
                    && EAFGetProperty(eafId, out var eafInfo) is EAF_SUCCESS && eafInfo.ID == eafId ? eafId : null as int?;
            }
            else if (typeof(TDeviceInfo) == typeof(EFW_INFO))
            {
                return EFWGetID(index, out var efwId) is EFW_SUCCESS
                    && EFWGetProperty(efwId, out var efwInfo) is EFW_SUCCESS && efwInfo.ID == efwId ? efwId : null as int?;
            }

            return null;
        }
    }
}
