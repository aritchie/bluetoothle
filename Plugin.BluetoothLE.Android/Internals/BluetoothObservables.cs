using System;
using System.Reactive.Linq;
using Android.Bluetooth;

namespace Plugin.BluetoothLE.Internals
{
    public static class BluetoothObservables
    {
        public static IObservable<object> WhenAdapterStatusChanged()
        {
            return AndroidObservables.WhenIntentReceived(BluetoothAdapter.ActionStateChanged);
        }


        public static IObservable<object> WhenAdapterDiscoveryStarted()
        {
            return AndroidObservables.WhenIntentReceived(BluetoothAdapter.ActionDiscoveryStarted);
        }


        public static IObservable<object> WhenAdapterDiscoveryFinished()
        {
            return AndroidObservables.WhenIntentReceived(BluetoothAdapter.ActionDiscoveryFinished);
        }


        public static IObservable<BluetoothDevice> WhenBondRequestReceived()
        {
            return WhenDeviceEventReceived(BluetoothDevice.ActionPairingRequest);
        }


        public static IObservable<BluetoothDevice> WhenBondStatusChanged()
        {
            return WhenDeviceEventReceived(BluetoothDevice.ActionBondStateChanged);
        }


        public static IObservable<BluetoothDevice> WhenDeviceNameChanged()
        {
            return WhenDeviceEventReceived(BluetoothDevice.ActionNameChanged);
        }


        public static IObservable<BluetoothDevice> WhenDeviceEventReceived(string action)
        {
            return AndroidObservables
                .WhenIntentReceived(action)
                .Select(intent =>
                {
                    var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    return device;
                });
        }
    }
}