using System.Collections;
using System.Collections.Generic;

namespace ZWOptical.SDK
{
    public class DeviceIterator<TDeviceType> : IEnumerable<int>
        where TDeviceType : struct, IZWODeviceInfo
    {
        public IEnumerator<int> GetEnumerator()
        {
            var count = DeviceCount();

            for (var i = 0; i < count; i++)
            {
                var id = GetId(i);
                if (id.HasValue)
                {
                    yield return id.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public int DeviceCount()
        {
            if (typeof(TDeviceType) == typeof(ASICamera2.ASI_CAMERA_INFO))
            {
                return ASICamera2.ASIGetNumOfConnectedCameras();
            }
            else if (typeof(TDeviceType) == typeof(EAFFocuser1_6.EAF_INFO))
            {
                return EAFFocuser1_6.EAFGetNum();
            }
            else if (typeof(TDeviceType) == typeof(EFW1_7.EFW_INFO))
            {
                return EFW1_7.EFWGetNum();
            }

            return 0;
        }

        public int? GetId(int index)
        {
            if (typeof(TDeviceType) == typeof(ASICamera2.ASI_CAMERA_INFO))
            {
                return ASICamera2.ASIGetCameraProperty(out var camInfo, index) is ASICamera2.ASI_ERROR_CODE.ASI_SUCCESS ? camInfo.CameraID : null as int?;
            }
            else if (typeof(TDeviceType) == typeof(EAFFocuser1_6.EAF_INFO))
            {
                return EAFFocuser1_6.EAFGetID(index, out var eafId) is EAFFocuser1_6.EAF_ERROR_CODE.EAF_SUCCESS ? eafId : null as int?;
            }
            else if (typeof(TDeviceType) == typeof(EFW1_7.EFW_INFO))
            {
                return EFW1_7.EFWGetID(index, out var efwId) is EFW1_7.EFW_ERROR_CODE.EFW_SUCCESS ? efwId : null as int?;
            }

            return null;
        }
    }
}
