namespace ZWOptical.SDK
{
    public interface IZWODeviceInfo
    {
        int ID { get; }

        string Name { get; }

        bool Open();

        bool Close();

        SDK_ID? SerialNumber { get; }

        bool IsUSB3Device { get; }
    }
}
