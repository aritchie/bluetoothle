using System;
using Acr.Logging;
using Android.Bluetooth.LE;


namespace Plugin.BluetoothLE.Server.Internals
{
    public class AdvertisementCallbacks : AdvertiseCallback
    {
        public Action Started { get; set; }
        public Action<Exception> Failed { get; set; }


        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            Log.Info(BleLogCategory.Advertiser, $"Succeeded to start BLE advertising - {settingsInEffect.Mode}");
            base.OnStartSuccess(settingsInEffect);
            this.Started?.Invoke();
        }


        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            Log.Warn(BleLogCategory.Advertiser, $"Failed to start BLE advertising - {errorCode}");
            base.OnStartFailure(errorCode);
            if (errorCode != AdvertiseFailure.AlreadyStarted) //doesn't seem to matter?
                this.Failed?.Invoke(new BleException($"Failed to start BLE advertising - {errorCode}"));
        }
    }
}
