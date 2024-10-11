using System.Collections;
using System.Collections.Generic;

namespace ZWOptical.SDK
{
    public class DeviceIterator : IEnumerable<int>
    {
        private readonly ZWODeviceType _deviceType;

        public DeviceIterator(ZWODeviceType deviceType)
        {
            _deviceType = deviceType;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var count = _deviceType.DeviceCount();

            for (var i = 0; i < count; i++)
            {
                var id = _deviceType.GetId(i);
                if (id.HasValue)
                {
                    yield return id.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
