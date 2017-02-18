using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Plugin.BluetoothLE
{
    public static class HeartRateExtensions
    {
        public static Guid HeartRateServiceUuid = new Guid("0000180d-0000-1000-8000-00805f9b34fb");


        /// <summary>
        /// Scan for heart rate sensors.  Note that a lot of heart rate sensors do not advertise their service UUID
        /// </summary>
        /// <param name="adapter"></param>
        /// <returns></returns>
        public static IObservable<IScanResult> ScanForHeartRateSensors(this IAdapter adapter)
        {
            return adapter
                .Scan()
                .Where(x => x.AdvertisementData.ServiceUuids?.Contains(HeartRateServiceUuid) ?? false);
        }


        public static async Task<bool> HasHeartSensor(this IDevice device)
        {
            AssertConnected(device);
            var character = await FindCharacteristic(device);
            return character != null;
        }


        public static IObservable<ushort> WhenHeartRateBpm(this IDevice device)
        {
            AssertConnected(device);

            return Observable.Create<ushort>(async ob =>
            {
                IDisposable token = null;
                var ch = await FindCharacteristic(device);

                if (ch == null)
                {
                    ob.OnError(new ArgumentException("Device does not appear to be a heart rate sensor"));
                }
                else
                {
                    token = ch
                        .WhenReadOrNotify(TimeSpan.FromSeconds(3))
                        .Subscribe(result =>
                        {
                            if ((result.Data[0] & 0x01) == 0)
                                ob.OnNext(result.Data[1]);

                            var bpm = (ushort)result.Data [1];
                            bpm = (ushort)(((bpm >> 8) & 0xFF) | ((bpm << 8) & 0xFF00));
                            ob.OnNext(bpm);
                        });
                }
                return () => token?.Dispose();
            });
        }


        static void AssertConnected(IDevice device)
        {
            if (device.Status != ConnectionStatus.Connected)
                throw new ArgumentException("Device must be connected");
        }


        static async Task<IGattCharacteristic> FindCharacteristic(IDevice device)
        {
            return await device
                .WhenServiceDiscovered()
                .Where(x => x.Uuid.Equals(HeartRateServiceUuid))
                .SelectMany(x => x.WhenCharacteristicDiscovered())
                .FirstOrDefaultAsync();
        }
    }
}
