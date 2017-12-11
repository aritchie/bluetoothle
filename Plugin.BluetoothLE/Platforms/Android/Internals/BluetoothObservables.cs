using System;
using System.Reactive.Linq;
using Android.Bluetooth;

namespace Plugin.BluetoothLE.Internals
{
    public static class BluetoothObservables
    {
        public static IObservable<object> WhenAdapterStatusChanged()
            => AndroidObservables.WhenIntentReceived(BluetoothAdapter.ActionStateChanged);

        public static IObservable<object> WhenAdapterDiscoveryStarted()
            => AndroidObservables.WhenIntentReceived(BluetoothAdapter.ActionDiscoveryStarted);

        public static IObservable<object> WhenAdapterDiscoveryFinished()
            => AndroidObservables.WhenIntentReceived(BluetoothAdapter.ActionDiscoveryFinished);

        public static IObservable<BluetoothDevice> WhenBondRequestReceived()
            => WhenDeviceEventReceived(BluetoothDevice.ActionPairingRequest);

        public static IObservable<BluetoothDevice> WhenBondStatusChanged()
            => WhenDeviceEventReceived(BluetoothDevice.ActionBondStateChanged);

        public static IObservable<BluetoothDevice> WhenDeviceNameChanged()
            => WhenDeviceEventReceived(BluetoothDevice.ActionNameChanged);

        public static IObservable<BluetoothDevice> WhenDeviceEventReceived(string action)
            => AndroidObservables
                .WhenIntentReceived(action)
                .Select(intent =>
                {
                    var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    return device;
                });
    }
}