namespace ZWOptical.SDK
{
    public interface IZWODeviceInfo
    {
        int ID { get; }

        string Name { get; }

        /// <summary>
        /// Custom ID is the same as <see cref="Name"/> except for USB 3 cameras.
        /// </summary>
        string CustomId { get; }

        bool Open();

        bool Close();

        SDK_ID? SerialNumber { get; }

        bool IsUSB3Device { get; }
    }
}
