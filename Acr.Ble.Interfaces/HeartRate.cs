using System;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Acr.Ble
{
    public static class HeartRateExtensions
    {
        public static Guid HeartRateServiceUuid = new Guid("0000180d-0000-1000-8000-00805f9b34fb");


        public static async Task<bool> HasHeartSensor(this IDevice device)
        {
            AssertConnected(device);
            var character = await FindCharacteristic(device);
            return character != null;
        }


        public static IObservable<ushort> MonitorHeartRateBpm(this IDevice device)
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
                else if (ch.CanNotify())
                {
                    token = ch
                        .WhenNotificationOccurs()
                        .Subscribe(data => DecodeHeartRate(ob, data));
                }
                else
                {
                    token = ch
                        .ReadInterval(TimeSpan.FromSeconds(3))
                        .Subscribe(data => DecodeHeartRate(ob, data));
                }

                return () => token?.Dispose();
            });
        }


        static void DecodeHeartRate(IObserver<ushort> ob, byte[] data)
        {
			if ((data[0] & 0x01) == 0)
				ob.OnNext(data[1]);

            var bpm = (ushort)data [1];
			bpm = (ushort)(((bpm >> 8) & 0xFF) | ((bpm << 8) & 0xFF00));
            ob.OnNext(bpm);
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
