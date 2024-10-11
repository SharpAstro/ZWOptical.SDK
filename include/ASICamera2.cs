using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.SDK
{
    public static class ASICamera2
    {
        public enum ASI_CONTROL_TYPE
        {
            ASI_GAIN = 0,
            ASI_EXPOSURE,
            ASI_GAMMA,
            ASI_WB_R,
            ASI_WB_B,
            ASI_BRIGHTNESS,
            ASI_BANDWIDTHOVERLOAD,
            ASI_OVERCLOCK,
            ASI_TEMPERATURE,// return 10*temperature
            ASI_FLIP,
            ASI_AUTO_MAX_GAIN,
            ASI_AUTO_MAX_EXP,
            ASI_AUTO_MAX_BRIGHTNESS,
            ASI_HARDWARE_BIN,
            ASI_HIGH_SPEED_MODE,
            ASI_COOLER_POWER_PERC,
            ASI_TARGET_TEMP,// not need *10
            ASI_COOLER_ON,
            ASI_MONO_BIN,
            ASI_FAN_ON,
            ASI_PATTERN_ADJUST,
            ASI_ANTI_DEW_HEATER,
            ASI_HUMIDITY,
            ASI_ENABLE_DDR
        }


        public enum ASI_IMG_TYPE
        {
            //Supported image type
            ASI_IMG_RAW8 = 0,
            ASI_IMG_RGB24,
            ASI_IMG_RAW16,
            ASI_IMG_Y8,
            ASI_IMG_END = -1
        }


        public enum ASI_GUIDE_DIRECTION
        {
            ASI_GUIDE_NORTH = 0,
            ASI_GUIDE_SOUTH,
            ASI_GUIDE_EAST,
            ASI_GUIDE_WEST
        }

        public enum ASI_BAYER_PATTERN
        {
            ASI_BAYER_RG = 0,
            ASI_BAYER_BG,
            ASI_BAYER_GR,
            ASI_BAYER_GB
        };

        public enum ASI_EXPOSURE_STATUS
        {
            ASI_EXP_IDLE = 0,//: idle states, you can start exposure now
            ASI_EXP_WORKING,//: exposing
            ASI_EXP_SUCCESS,// exposure finished and waiting for download
            ASI_EXP_FAILED,//:exposure failed, you need to start exposure again
        };

        public enum ASI_ERROR_CODE
        { //ASI ERROR CODE
            ASI_SUCCESS = 0,
            ASI_ERROR_INVALID_INDEX, //no camera connected or index value out of boundary
            ASI_ERROR_INVALID_ID, //invalid ID
            ASI_ERROR_INVALID_CONTROL_TYPE, //invalid control type
            ASI_ERROR_CAMERA_CLOSED, //camera didn't open
            ASI_ERROR_CAMERA_REMOVED, //failed to find the camera, maybe the camera has been removed
            ASI_ERROR_INVALID_PATH, //cannot find the path of the file
            ASI_ERROR_INVALID_FILEFORMAT,
            ASI_ERROR_INVALID_SIZE, //wrong video format size
            ASI_ERROR_INVALID_IMGTYPE, //unsupported image formate
            ASI_ERROR_OUTOF_BOUNDARY, //the startpos is out of boundary
            ASI_ERROR_TIMEOUT, //timeout
            ASI_ERROR_INVALID_SEQUENCE,//stop capture first
            ASI_ERROR_BUFFER_TOO_SMALL, //buffer size is not big enough
            ASI_ERROR_VIDEO_MODE_ACTIVE,
            ASI_ERROR_EXPOSURE_IN_PROGRESS,
            ASI_ERROR_GENERAL_ERROR,//general error, eg: value is out of valid range
            ASI_ERROR_END
        };
        public enum ASI_BOOL
        {
            ASI_FALSE = 0,
            ASI_TRUE
        };
        public enum ASI_FLIP_STATUS
        {
            ASI_FLIP_NONE = 0,//: original
            ASI_FLIP_HORIZ,//: horizontal flip
            ASI_FLIP_VERT,// vertical flip
            ASI_FLIP_BOTH,//:both horizontal and vertical flip

        };
        public struct ASI_CAMERA_INFO : IZWODeviceInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            private byte[] _name;// char[64]; //the name of the camera, you can display this to the UI
            public int CameraID; //this is used to control everything of the camera in other functions
            public int MaxHeight; //the max height of the camera
            public int MaxWidth;	//the max width of the camera

            public ASI_BOOL IsColorCam;
            public ASI_BAYER_PATTERN BayerPattern;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public int[] SupportedBins;// int[16]; //1 means bin1 which is supported by every camera, 2 means bin 2 etc.. 0 is the end of supported binning method

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public ASI_IMG_TYPE[] SupportedVideoFormat;// ASI_IMG_TYPE[8]; //this array will content with the support output format type.IMG_END is the end of supported video format

            public double PixelSize; //the pixel size of the camera, unit is um. such like 5.6um
            public ASI_BOOL MechanicalShutter;
            public ASI_BOOL ST4Port;
            public ASI_BOOL IsCoolerCam;
            public ASI_BOOL IsUSB3Host;
            public ASI_BOOL IsUSB3Camera;
            public float ElecPerADU;

            /// <summary>
            /// Actual bit depth
            /// </summary>
            public int BitDepth;

            public ASI_BOOL IsTriggerCam;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Native struct padding")]
            private readonly byte[] _unused;

            public int ID => CameraID;

            public string Name => Encoding.ASCII.GetString(_name).TrimEnd((char)0);

            public bool Open() => ASIOpenCamera(ID) is ASI_ERROR_CODE.ASI_SUCCESS;

            public bool Close() => ASICloseCamera(ID) is ASI_ERROR_CODE.ASI_SUCCESS;

            public SDK_ID? SerialNumber => ASIGetSerialNumber(ID, out var sn) is ASI_ERROR_CODE.ASI_SUCCESS ? sn : null as SDK_ID?;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ASI_CONTROL_CAPS
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            public byte[] name; //the name of the Control like Exposure, Gain etc..
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 128)]
            public byte[] description; //description of this control
            public int MaxValue;
            public int MinValue;
            public int DefaultValue;
            public ASI_BOOL IsAutoSupported; //support auto set 1, don't support 0
            public ASI_BOOL IsWritable; //some control like temperature can only be read by some cameras
            public ASI_CONTROL_TYPE ControlType;//this is used to get value and set value of the control
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
            public byte[] Unused;//[32];

            public string Name
            {
                get { return Encoding.ASCII.GetString(name).TrimEnd((Char)0); }
            }

            public string Description
            {
                get { return Encoding.ASCII.GetString(description).TrimEnd((Char)0); }
            }
        }

        public enum ASI_CAMERA_MODE
        {
            ASI_MODE_NORMAL = 0,
            ASI_MODE_TRIG_SOFT_EDGE,
            ASI_MODE_TRIG_RISE_EDGE,
            ASI_MODE_TRIG_FALL_EDGE,
            ASI_MODE_TRIG_SOFT_LEVEL,
            ASI_MODE_TRIG_HIGH_LEVEL,
            ASI_MODE_TRIG_LOW_LEVEL,
            ASI_MODE_END = -1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ASI_SUPPORTED_MODE
        {
            /// <summary>
            /// // this array will content with the support camera mode type.ASI_MODE_END is the end of supported camera mode.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public ASI_CAMERA_MODE[] SupportedCameraMode;
        }

        [DllImport("ASICamera2", EntryPoint = "ASIGetNumOfConnectedCameras", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ASIGetNumOfConnectedCameras();

        [DllImport("ASICamera2", EntryPoint = "ASIGetCameraProperty", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetCameraProperty(out ASI_CAMERA_INFO pASICameraInfo, int iCameraIndex);

        [DllImport("ASICamera2", EntryPoint = "ASIOpenCamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIOpenCamera(int iCameraID);

        [DllImport("ASICamera2", EntryPoint = "ASIInitCamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIInitCamera(int iCameraID);

        [DllImport("ASICamera2", EntryPoint = "ASICloseCamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASICloseCamera(int iCameraID);

        [DllImport("ASICamera2", EntryPoint = "ASIGetNumOfControls", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetNumOfControls(int iCameraID, out int piNumberOfControls);

        [DllImport("ASICamera2", EntryPoint = "ASIGetControlCaps", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetControlCaps(int iCameraID, int iControlIndex, out ASI_CONTROL_CAPS pControlCaps);

        [DllImport("ASICamera2", EntryPoint = "ASISetControlValue", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASISetControlValueImpl(int iCameraID, ASI_CONTROL_TYPE ControlType, int lValue, ASI_BOOL bAuto);

        [DllImport("ASICamera2", EntryPoint = "ASIGetControlValue", CallingConvention = CallingConvention.Cdecl)]
        private static extern ASI_ERROR_CODE ASIGetControlValueImpl(int iCameraID, ASI_CONTROL_TYPE ControlType, out int plValue, out ASI_BOOL pbAuto);

        [DllImport("ASICamera2", EntryPoint = "ASISetROIFormat", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASISetROIFormat(int iCameraID, int iWidth, int iHeight, int iBin, ASI_IMG_TYPE Img_type);

        [DllImport("ASICamera2", EntryPoint = "ASIGetROIFormat", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetROIFormat(int iCameraID, out int piWidth, out int piHeight, out int piBin, out ASI_IMG_TYPE pImg_type);

        [DllImport("ASICamera2", EntryPoint = "ASIGetSDKVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ASIGetSDKVersionImpl();

        [DllImport("ASICamera2", EntryPoint = "ASIGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE  ASIGetSerialNumber(int iCameraID, out SDK_ID pSN);

        [DllImport("ASICamera2", EntryPoint = "ASISetStartPos", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASISetStartPos(int iCameraID, int iStartX, int iStartY);

        [DllImport("ASICamera2", EntryPoint = "ASIGetStartPos", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetStartPos(int iCameraID, out int piStartX, out int piStartY);

        [DllImport("ASICamera2", EntryPoint = "ASIStartVideoCapture", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIStartVideoCapture(int iCameraID);

        [DllImport("ASICamera2", EntryPoint = "ASIStopVideoCapture", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIStopVideoCapture(int iCameraID);

        [DllImport("ASICamera2", EntryPoint = "ASIGetVideoData", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetVideoData(int iCameraID, IntPtr pBuffer, int lBuffSize, int iWaitms);

        [DllImport("ASICamera2", EntryPoint = "ASIPulseGuideOn", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIPulseGuideOn(int iCameraID, ASI_GUIDE_DIRECTION direction);

        [DllImport("ASICamera2", EntryPoint = "ASIPulseGuideOff", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIPulseGuideOff(int iCameraID, ASI_GUIDE_DIRECTION direction);

        [DllImport("ASICamera2", EntryPoint = "ASIStartExposure", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIStartExposure(int iCameraID, ASI_BOOL bIsDark);

        [DllImport("ASICamera2", EntryPoint = "ASIStopExposure", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIStopExposure(int iCameraID);

        [DllImport("ASICamera2", EntryPoint = "ASIGetExpStatus", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetExpStatus(int iCameraID, out ASI_EXPOSURE_STATUS pExpStatus);

        [DllImport("ASICamera2", EntryPoint = "ASIGetDataAfterExp", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetDataAfterExp(int iCameraID, IntPtr pBuffer, int lBuffSize);

        [DllImport("ASICamera2", EntryPoint = "ASIGetGainOffset", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetGainOffset(int iCameraID, out int Offset_HighestDR, out int Offset_UnityGain, out int Gain_LowestRN, out int Offset_LowestRN);

        [DllImport("ASICamera2", EntryPoint = "ASIGetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetID(int iCameraID, out SDK_ID pID);

        [DllImport("ASICamera2", EntryPoint = "ASISetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASISetID(int iCameraID, SDK_ID ID);

        /// <summary>
        /// Starts an exposure with an open <see cref="ASI_CAMERA_INFO.MechanicalShutter"/> (i.e. a light exposure).
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns><see cref="ASI_ERROR_CODE.ASI_SUCCESS"/> if exposure was started successfully.</returns>
        public static ASI_ERROR_CODE ASIStartLightExposure(int iCameraID) => ASIStartExposure(iCameraID, ASI_BOOL.ASI_FALSE);

        /// <summary>
        /// Starts an exposure with a closed <see cref="ASI_CAMERA_INFO.MechanicalShutter"/>.
        /// Is the same as <see cref="ASIStartLightExposure"/> if above property is <see cref="ASI_BOOL.ASI_FALSE"/>.
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns><see cref="ASI_ERROR_CODE.ASI_SUCCESS"/> if exposure was started successfully.</returns>
        public static ASI_ERROR_CODE ASIStartDarkExposure(int iCameraID) => ASIStartExposure(iCameraID, ASI_BOOL.ASI_TRUE);

        /// <summary>
        /// Get the camera supported mode, only need to call when the <see cref="ASI_CAMERA_INFO.IsTriggerCam"/> in the <see cref="ASI_CAMERA_INFO"/> is  <see langword="true"/>
        /// </summary>
        /// <param name="iCameraID">this is get from the camera property use the API ASIGetCameraProperty</param>
        /// <param name="pSupportedMode">the camera supported mode</param>
        /// <returns>
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Return code</term>
        ///       <description>reason</description>
        ///     </listheader>
        ///     <item>
        ///       <term>ASI_SUCCESS</term>
        ///       <description>Operation is successful</description>
        ///     </item>
        ///     <item>
        ///       <term>ASI_ERROR_CAMERA_CLOSED</term>
        ///       <description>camera did not open</description>
        ///     </item>
        ///     <item>
        ///       <term>ASI_ERROR_INVALID_ID</term>
        ///       <description>no camera of this ID is connected or ID value is out of boundary</description>
        ///     </item>
        ///   </list>
        /// </returns>
        [DllImport("ASICamera2", EntryPoint = "ASIGetCameraSupportMode", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetCameraSupportMode(int iCameraID, out ASI_SUPPORTED_MODE pSupportedMode);

        /// <summary>
        /// Get the camera current mode, only need to call when the <see cref="ASI_CAMERA_INFO.IsTriggerCam"/> in the <see cref="ASI_CAMERA_INFO"/> is  <see langword="true"/>
        /// </summary>
        /// <param name="iCameraID">this is get from the camera property use the API ASIGetCameraProperty</param>
        /// <param name="mode">the current camera mode</param>
        /// <returns>
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Return code</term>
        ///       <description>reason</description>
        ///     </listheader>
        ///     <item>
        ///       <term>ASI_SUCCESS</term>
        ///       <description>Operation is successful</description>
        ///     </item>
        ///     <item>
        ///       <term>ASI_ERROR_CAMERA_CLOSED</term>
        ///       <description>camera did not open</description>
        ///     </item>
        ///     <item>
        ///       <term>ASI_ERROR_INVALID_ID</term>
        ///       <description>no camera of this ID is connected or ID value is out of boundary</description>
        ///     </item>
        ///   </list>
        /// </returns>
        [DllImport("ASICamera2", EntryPoint = "ASIGetCameraMode", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASI_ERROR_CODE ASIGetCameraMode(int iCameraID, out ASI_CAMERA_MODE mode);

        public static ASI_ERROR_CODE ASISetControlValue(int iCameraID, ASI_CONTROL_TYPE ControlType, int lValue, bool isAuto = false)
            => ASISetControlValueImpl(iCameraID, ControlType, lValue, isAuto ? ASI_BOOL.ASI_TRUE : ASI_BOOL.ASI_FALSE);

        public static ASI_ERROR_CODE ASIGetControlValue(int iCameraID, ASI_CONTROL_TYPE ControlType, out int plValue, out bool isAuto)
        {
            ASI_ERROR_CODE err = ASIGetControlValueImpl(iCameraID, ControlType, out plValue, out ASI_BOOL pbAuto);
            isAuto = pbAuto is ASI_BOOL.ASI_TRUE;
            return err;
        }

        /// <summary>
        /// Returns SDK version with this format: <code>1, 51</code>
        /// </summary>
        /// <returns></returns>
        public static Version ASIGetSDKVersion() => Common.ParseVersionString(ASIGetSDKVersionImpl());
        public static bool TryGetControlRange(
            int iCameraID,
            ASI_CONTROL_TYPE ctrlType,
            out int min,
            out int max)
        {
            min = max = 0;
            if (ASIGetNumOfControls(iCameraID, out int numberOfControls) != ASI_ERROR_CODE.ASI_SUCCESS)
            {
                return false;
            }

            for (int controlIdx = 0; controlIdx < numberOfControls; ++controlIdx)
            {
                var controlCapsErrorCode = ASIGetControlCaps(iCameraID, controlIdx, out ASI_CONTROL_CAPS controlCaps);
                if (controlCapsErrorCode == ASI_ERROR_CODE.ASI_SUCCESS && controlCaps.ControlType == ctrlType)
                {
                    min = controlCaps.MinValue;
                    max = controlCaps.MaxValue;
                    return true;
                }
            }
            return false;
        }
    }
}
