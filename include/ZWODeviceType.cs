namespace ZWOptical.SDK
{
    public enum ZWODeviceType
    {
        Camera,
        EAF,
        EFW
    }

    public static class DeviceTypeEx
    {
        public static int DeviceCount(this ZWODeviceType deviceType)
        {
            switch (deviceType)
            {
                case ZWODeviceType.Camera:
                    return ASICamera2.ASIGetNumOfConnectedCameras();
                case ZWODeviceType.EAF:
                    return EAFFocuser1_6.EAFGetNum();
                case ZWODeviceType.EFW:
                    return EFW1_7.EFWGetNum();
                default:
                    return 0;
            };
        }

        public static int? GetId(this ZWODeviceType deviceType, int index)
        {
            switch (deviceType)
            {
                case ZWODeviceType.Camera:
                    return ASICamera2.ASIGetCameraProperty(out var camInfo, index) is ASICamera2.ASI_ERROR_CODE.ASI_SUCCESS ? camInfo.CameraID : null as int?;
                case ZWODeviceType.EAF:
                    return EAFFocuser1_6.EAFGetID(index, out var eafId) is EAFFocuser1_6.EAF_ERROR_CODE.EAF_SUCCESS ? eafId : null as int?;
                case ZWODeviceType.EFW:
                    return EFW1_7.EFWGetID(index, out var efwId) is EFW1_7.EFW_ERROR_CODE.EFW_SUCCESS ? efwId : null as int?;
                default:
                    return null;
            };
        }
    }
}
