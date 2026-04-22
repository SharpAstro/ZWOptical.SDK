using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TianWen.DAL;

namespace ZWOptical.SDK;

public static partial class ASICamera2
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ASI_CAMERA_INFO : ICMOSNativeInterface
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        private readonly byte[] _name;  // char[64]; //the name of the camera, you can display this to the UI
        private readonly int _cameraID;   // this is used to control everything of the camera in other functions
        private readonly int _maxHeight;  // the max height of the camera
        private readonly int _maxWidth;   // the max width of the camera

        private readonly ASI_BOOL _isColorCam;
        private readonly ASI_BAYER_PATTERN _bayerPattern;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private readonly int[] _supportedBins;// int[16]; //1 means bin1 which is supported by every camera, 2 means bin 2 etc.. 0 is the end of supported binning method

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private readonly ASI_IMG_TYPE[] _supportedVideoFormat;// ASI_IMG_TYPE[8]; //this array will content with the support output format type.IMG_END is the end of supported video format

        private readonly double _pixelSize; //the pixel size of the camera, unit is um. such like 5.6um
        private readonly ASI_BOOL _mechanicalShutter;
        private readonly ASI_BOOL _st4Port;
        private readonly ASI_BOOL _isCoolerCam;
        private readonly ASI_BOOL _isUSB3Host;
        private readonly ASI_BOOL _isUSB3Camera;
        private readonly float _elecPerADU;

        /// <summary>
        /// Actual bit depth
        /// </summary>
        private readonly int _bitDepth;

        private readonly ASI_BOOL _isTriggerCam;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
        private readonly byte[] _unused;

        public int ID => _cameraID;

        public string Name => Encoding.ASCII.GetString(_name).TrimEnd((char)0);

        public bool Open() => ASIOpenCamera(ID) is ASI_ERROR_CODE.ASI_SUCCESS;

        public bool Close() => ASICloseCamera(ID) is ASI_ERROR_CODE.ASI_SUCCESS;

        /// <summary>
        /// Factory-programmed serial as a 16-char hex string, or null if the camera
        /// has no serial number programmed (<c>ASI_ERROR_GENERAL_ERROR</c>). Per the
        /// ASICamera2.h docs: the native <c>ASI_SN</c> is 8 raw binary bytes "to be
        /// printed in hexadecimal", NOT ASCII-decoded text. An all-zero or all-0xFF
        /// serial (bodies that the firmware reports as SUCCESS despite having no SN)
        /// is also treated as missing.
        /// </summary>
        public string SerialNumber
        {
            get
            {
                if (ASIGetSerialNumber(ID, out var sn) is not ASI_ERROR_CODE.ASI_SUCCESS)
                    return null;
                var hex = sn.ToHexString();
                // Reject 0x00 x8 / 0xFF x8: some firmware returns SUCCESS with these
                // "empty" byte patterns on unprogrammed bodies instead of the error code.
                return hex is "0000000000000000" or "FFFFFFFFFFFFFFFF" ? null : hex;
            }
        }

        public bool IsUSB3Device => _isUSB3Camera is ASI_BOOL.ASI_TRUE;

        public bool IsTriggerCamera => _isTriggerCam is ASI_BOOL.ASI_TRUE;

        public BayerPattern BayerPattern => _isColorCam is ASI_BOOL.ASI_TRUE
            ? _bayerPattern switch
            {
                ASI_BAYER_PATTERN.ASI_BAYER_RG => BayerPattern.RGGB,
                ASI_BAYER_PATTERN.ASI_BAYER_BG => BayerPattern.BGGR,
                ASI_BAYER_PATTERN.ASI_BAYER_GR => BayerPattern.GRBG,
                ASI_BAYER_PATTERN.ASI_BAYER_GB => BayerPattern.GBRG,
                _ => throw new NotSupportedException($"Unsupported Bayer pattern: {_bayerPattern}")
            }
            : BayerPattern.Monochrome;

        public IReadOnlyList<int> SupportedBins => _supportedBins;

        public IReadOnlyList<PixelDataFormat> SupportedPixelDataFormats
        {
            get
            {
                var list = new List<PixelDataFormat>(_supportedVideoFormat.Length);

                for (var i = 0; i < _supportedVideoFormat.Length; i++)
                {
                    if (_supportedVideoFormat[i] is ASI_IMG_TYPE.ASI_IMG_END)
                        break;

                    list.Add(_supportedVideoFormat[i] switch
                    {
                        ASI_IMG_TYPE.ASI_IMG_RAW8 => PixelDataFormat.RAW8,
                        ASI_IMG_TYPE.ASI_IMG_RGB24 => PixelDataFormat.RGB24,
                        ASI_IMG_TYPE.ASI_IMG_RAW16 => PixelDataFormat.RAW16,
                        ASI_IMG_TYPE.ASI_IMG_Y8 => PixelDataFormat.Y8,
                        _ => throw new NotSupportedException($"Unsupported image type: {_supportedVideoFormat[i]}")
                    });
                }

                return list;
            }
        }

        public bool HasMechanicalShutter => _mechanicalShutter is ASI_BOOL.ASI_TRUE;

        public bool HasCooler => _isCoolerCam is ASI_BOOL.ASI_TRUE;

        public bool HasST4Port => _st4Port is ASI_BOOL.ASI_TRUE;

        public int MaxWidth => _maxWidth;

        public int MaxHeight => _maxHeight;

        public int BitDepth => _bitDepth;

        public double ElectronPerADU => _elecPerADU;

        public double PixelSize => _pixelSize;

        /// <summary>
        /// User-written 8-byte custom id (set via the ZWO ASICap tool on USB3 bodies),
        /// or <see cref="Name"/> if no valid printable custom id was programmed. Same
        /// garbage-rejection rule as <see cref="SerialNumber"/>.
        /// </summary>
        public string CustomId
            => ASIGetID(ID, out var id) is ASI_ERROR_CODE.ASI_SUCCESS
               && id.TryGetPrintableText(out var text)
                ? text
                : Name;

        public bool TryGetControlRange(CMOSControlType ctrlType, out int min, out int max)
        {
            min = max = 0;
            if (ASIGetNumOfControls(_cameraID, out int numberOfControls) is not ASI_ERROR_CODE.ASI_SUCCESS
                || !DALControlTypeToASI(ctrlType, out var asiControlType))
            {
                return false;
            }

            for (int controlIdx = 0; controlIdx < numberOfControls; ++controlIdx)
            {
                var controlCapsErrorCode = ASIGetControlCaps(_cameraID, controlIdx, out ASI_CONTROL_CAPS controlCaps);
                if (controlCapsErrorCode is ASI_ERROR_CODE.ASI_SUCCESS && controlCaps.ControlType == asiControlType)
                {
                    min = controlCaps.MinValue;
                    max = controlCaps.MaxValue;
                    return true;
                }
            }
            return false;
        }

        public CMOSErrorCode SetControlValue(CMOSControlType controlType, int value, bool isAuto = false)
        {
            if (DALControlTypeToASI(controlType, out ASI_CONTROL_TYPE asiControlType))
                return (CMOSErrorCode)ASISetControlValueImpl(_cameraID, asiControlType, value, isAuto ? ASI_BOOL.ASI_TRUE : ASI_BOOL.ASI_FALSE);

            throw new ArgumentException($"{controlType} is not supported", nameof(controlType));
        }

        public CMOSErrorCode GetControlValue(CMOSControlType controlType, out int value, out bool isAuto)
        {
            if (DALControlTypeToASI(controlType, out ASI_CONTROL_TYPE asiControlType))
            {
                var err = ASIGetControlValueImpl(_cameraID, asiControlType, out value, out ASI_BOOL pbAuto);
                isAuto = pbAuto is ASI_BOOL.ASI_TRUE;
                return (CMOSErrorCode)err;
            }

            throw new ArgumentException($"{controlType} is not supported", nameof(controlType));
        }

        public CMOSErrorCode PulseGuideOn(GuideDirection guideDirection) => (CMOSErrorCode)ASIPulseGuideOn(_cameraID, (ASI_GUIDE_DIRECTION)guideDirection);

        public CMOSErrorCode PulseGuideOff(GuideDirection guideDirection) => (CMOSErrorCode)ASIPulseGuideOff(_cameraID, (ASI_GUIDE_DIRECTION)guideDirection);

        public CMOSErrorCode StartLightExposure() => (CMOSErrorCode)ASIStartLightExposure(_cameraID);

        public CMOSErrorCode StartDarkExposure() => (CMOSErrorCode)ASIStartDarkExposure(_cameraID);

        public CMOSErrorCode StopExposure() => (CMOSErrorCode)ASIStopExposure(_cameraID);

        public CMOSErrorCode GetExposureStatus(out ExposureStatus exposureStatus)
        {
            var err = ASIGetExpStatus(_cameraID, out var asiExpStatus);

            exposureStatus = err is ASI_ERROR_CODE.ASI_SUCCESS
                ? (ExposureStatus)asiExpStatus
                : (ExposureStatus)(-1);

            return (CMOSErrorCode)err;
        }

        public CMOSErrorCode GetStartPosition(out int startX, out int startY) => (CMOSErrorCode)ASIGetStartPos(_cameraID, out startX, out startY);

        public CMOSErrorCode SetStartPosition(int startX, int startY) => (CMOSErrorCode)ASISetStartPos(_cameraID, startX, startY);

        public CMOSErrorCode GetROIFormat(out int width, out int height, out int bin, out PixelDataFormat pixelDataFormat)
        {
            var err = ASIGetROIFormat(_cameraID, out width, out height, out bin, out var asiImgType);

            pixelDataFormat = err is ASI_ERROR_CODE.ASI_SUCCESS
                ? (PixelDataFormat)asiImgType
                : (PixelDataFormat)(-1);

            return (CMOSErrorCode)err;
        }

        public CMOSErrorCode SetROIFormat(int width, int height, int bin, PixelDataFormat pixelDataFormat) => (CMOSErrorCode)ASISetROIFormat(_cameraID, width, height, bin, (ASI_IMG_TYPE)pixelDataFormat);

        public CMOSErrorCode GetDataAfterExposure(IntPtr buffer, int bufferSize) => (CMOSErrorCode)ASIGetDataAfterExp(_cameraID, buffer, bufferSize);
    };

    public static bool DALControlTypeToASI(CMOSControlType dalValue, out ASI_CONTROL_TYPE asiValue)
    {
        asiValue = dalValue switch
        {
            CMOSControlType.Gain => ASI_CONTROL_TYPE.ASI_GAIN,
            CMOSControlType.Exposure => ASI_CONTROL_TYPE.ASI_EXPOSURE,
            CMOSControlType.Gamma => ASI_CONTROL_TYPE.ASI_GAMMA,
            CMOSControlType.WB_R => ASI_CONTROL_TYPE.ASI_WB_R,
            CMOSControlType.WB_B => ASI_CONTROL_TYPE.ASI_WB_B,
            CMOSControlType.Brightness => ASI_CONTROL_TYPE.ASI_BRIGHTNESS,
            CMOSControlType.BandwidthOverload => ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD,
            CMOSControlType.Overclock => ASI_CONTROL_TYPE.ASI_OVERCLOCK,
            CMOSControlType.Flip => ASI_CONTROL_TYPE.ASI_FLIP,
            CMOSControlType.AutoMaxGain => ASI_CONTROL_TYPE.ASI_AUTO_MAX_GAIN,
            CMOSControlType.AutoMaxExposure => ASI_CONTROL_TYPE.ASI_AUTO_MAX_EXP,
            CMOSControlType.AutoMaxBrightness => ASI_CONTROL_TYPE.ASI_AUTO_MAX_BRIGHTNESS,
            CMOSControlType.HardwareBin => ASI_CONTROL_TYPE.ASI_HARDWARE_BIN,
            CMOSControlType.HighSpeedMode => ASI_CONTROL_TYPE.ASI_HIGH_SPEED_MODE,
            CMOSControlType.CoolerPowerPercent => ASI_CONTROL_TYPE.ASI_COOLER_POWER_PERC,
            CMOSControlType.TargetTemperature => ASI_CONTROL_TYPE.ASI_TARGET_TEMP,
            CMOSControlType.CoolerOn => ASI_CONTROL_TYPE.ASI_COOLER_ON,
            CMOSControlType.MonoBin => ASI_CONTROL_TYPE.ASI_MONO_BIN,
            CMOSControlType.FanOn => ASI_CONTROL_TYPE.ASI_FAN_ON,
            CMOSControlType.PatternAdjust => ASI_CONTROL_TYPE.ASI_PATTERN_ADJUST,
            CMOSControlType.AntiDewHeater => ASI_CONTROL_TYPE.ASI_ANTI_DEW_HEATER,
            CMOSControlType.Humidity => ASI_CONTROL_TYPE.ASI_HUMIDITY,
            CMOSControlType.EnableDDR => ASI_CONTROL_TYPE.ASI_ENABLE_DDR,
            _ => (ASI_CONTROL_TYPE)int.MaxValue
        };

        return asiValue is not (ASI_CONTROL_TYPE)int.MaxValue;
    }

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
    {
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
        ASI_FLIP_HORIZ,   //: horizontal flip
        ASI_FLIP_VERT,    //: vertical flip
        ASI_FLIP_BOTH,    //: both horizontal and vertical flip

    };

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ASI_CONTROL_CAPS
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        private readonly byte[] _name; //the name of the Control like Exposure, Gain etc..
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 128)]
        private readonly byte[] _description; //description of this control
        private readonly int _maxValue;
        private readonly int _minValue;
        private readonly int _defaultValue;
        private readonly ASI_BOOL _isAutoSupported; //support auto set 1, don't support 0
        private readonly ASI_BOOL _isWritable; //some control like temperature can only be read by some cameras
        private readonly ASI_CONTROL_TYPE _controlType;//this is used to get value and set value of the control
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
        private readonly byte[] _unused;//[32];

        public string Name => Encoding.ASCII.GetString(_name).TrimEnd((char)0);

        public string Description => Encoding.ASCII.GetString(_description).TrimEnd((char)0);

        public int MaxValue => _maxValue;
        public int MinValue => _minValue;
        public int DefaultValue => _defaultValue;
        public bool IsAutoSupported => _isAutoSupported is ASI_BOOL.ASI_TRUE; //support auto set 1, don't support 0
        public bool IsWritable => _isWritable is ASI_BOOL.ASI_TRUE; //some control like temperature can only be read by some cameras
        public ASI_CONTROL_TYPE ControlType => _controlType;//this is used to get value and set value of the control

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

    const string ASISharedLib = "ASICamera2";

    // Functions with non-blittable struct parameters use DllImport
    [DllImport(ASISharedLib, EntryPoint = "ASIGetCameraProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern ASI_ERROR_CODE ASIGetCameraProperty(out ASI_CAMERA_INFO pASICameraInfo, int iCameraIndex);

    [DllImport(ASISharedLib, EntryPoint = "ASIGetControlCaps", CallingConvention = CallingConvention.Cdecl)]
    private static extern ASI_ERROR_CODE ASIGetControlCaps(int iCameraID, int iControlIndex, out ASI_CONTROL_CAPS pControlCaps);

    [DllImport(ASISharedLib, EntryPoint = "ASIGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
    public static extern ASI_ERROR_CODE ASIGetSerialNumber(int iCameraID, out ZWO_ID pSN);

    [DllImport(ASISharedLib, EntryPoint = "ASIGetID", CallingConvention = CallingConvention.Cdecl)]
    public static extern ASI_ERROR_CODE ASIGetID(int iCameraID, out ZWO_ID pID);

    [DllImport(ASISharedLib, EntryPoint = "ASISetID", CallingConvention = CallingConvention.Cdecl)]
    public static extern ASI_ERROR_CODE ASISetID(int iCameraID, ZWO_ID ID);

    [DllImport(ASISharedLib, EntryPoint = "ASIGetCameraSupportMode", CallingConvention = CallingConvention.Cdecl)]
    public static extern ASI_ERROR_CODE ASIGetCameraSupportMode(int iCameraID, out ASI_SUPPORTED_MODE pSupportedMode);

    // Functions with blittable parameters use LibraryImport
    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetNumOfConnectedCameras")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ASIGetNumOfConnectedCameras();

    [LibraryImport(ASISharedLib, EntryPoint = "ASIOpenCamera")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIOpenCamera(int iCameraID);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIInitCamera")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIInitCamera(int iCameraID);

    [LibraryImport(ASISharedLib, EntryPoint = "ASICloseCamera")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASICloseCamera(int iCameraID);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetNumOfControls")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetNumOfControls(int iCameraID, out int piNumberOfControls);

    [LibraryImport(ASISharedLib, EntryPoint = "ASISetControlValue")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ASI_ERROR_CODE ASISetControlValueImpl(int iCameraID, ASI_CONTROL_TYPE ControlType, int lValue, ASI_BOOL bAuto);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetControlValue")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ASI_ERROR_CODE ASIGetControlValueImpl(int iCameraID, ASI_CONTROL_TYPE ControlType, out int plValue, out ASI_BOOL pbAuto);

    [LibraryImport(ASISharedLib, EntryPoint = "ASISetROIFormat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASISetROIFormat(int iCameraID, int iWidth, int iHeight, int iBin, ASI_IMG_TYPE Img_type);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetROIFormat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetROIFormat(int iCameraID, out int piWidth, out int piHeight, out int piBin, out ASI_IMG_TYPE pImg_type);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetSDKVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr ASIGetSDKVersionImpl();

    [LibraryImport(ASISharedLib, EntryPoint = "ASISetStartPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASISetStartPos(int iCameraID, int iStartX, int iStartY);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetStartPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetStartPos(int iCameraID, out int piStartX, out int piStartY);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIStartVideoCapture")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIStartVideoCapture(int iCameraID);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIStopVideoCapture")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIStopVideoCapture(int iCameraID);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetVideoData")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetVideoData(int iCameraID, IntPtr pBuffer, int lBuffSize, int iWaitms);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIPulseGuideOn")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIPulseGuideOn(int iCameraID, ASI_GUIDE_DIRECTION direction);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIPulseGuideOff")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIPulseGuideOff(int iCameraID, ASI_GUIDE_DIRECTION direction);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIStartExposure")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIStartExposure(int iCameraID, ASI_BOOL bIsDark);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIStopExposure")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIStopExposure(int iCameraID);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetExpStatus")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetExpStatus(int iCameraID, out ASI_EXPOSURE_STATUS pExpStatus);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetDataAfterExp")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetDataAfterExp(int iCameraID, IntPtr pBuffer, int lBuffSize);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetGainOffset")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetGainOffset(int iCameraID, out int Offset_HighestDR, out int Offset_UnityGain, out int Gain_LowestRN, out int Offset_LowestRN);

    [LibraryImport(ASISharedLib, EntryPoint = "ASIGetCameraMode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASIGetCameraMode(int iCameraID, out ASI_CAMERA_MODE mode);

    [LibraryImport(ASISharedLib, EntryPoint = "ASISetCameraMode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASISetCameraMode(int iCameraID, ASI_CAMERA_MODE mode);

    [LibraryImport(ASISharedLib, EntryPoint = "ASISendSoftTrigger")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ASI_ERROR_CODE ASISendSoftTrigger(int iCameraID, ASI_BOOL bStart);

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
    /// Returns SDK version with this format: <code>1, 51</code>
    /// </summary>
    /// <returns></returns>
    public static Version ASIGetSDKVersion() => Common.ParseVersionString(ASIGetSDKVersionImpl());
}
