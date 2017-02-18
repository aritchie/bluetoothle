using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class GattCallbacks : BluetoothGattCallback
    {

        public event EventHandler<GattCharacteristicEventArgs> CharacteristicRead;
        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            this.CharacteristicRead?.Invoke(this, new GattCharacteristicEventArgs(gatt, characteristic, status));
        }


        public event EventHandler<GattCharacteristicEventArgs> CharacteristicWrite;
        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            this.CharacteristicWrite?.Invoke(this, new GattCharacteristicEventArgs(gatt, characteristic, status));
        }


        public event EventHandler<GattCharacteristicEventArgs> CharacteristicChanged;
        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            this.CharacteristicChanged?.Invoke(this, new GattCharacteristicEventArgs(gatt, characteristic));
        }


        public event EventHandler<GattDescriptorEventArgs> DescriptorRead;
        public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            this.DescriptorRead?.Invoke(this, new GattDescriptorEventArgs(gatt, descriptor, status));
        }


        public event EventHandler<GattDescriptorEventArgs> DescriptorWrite;
        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            this.DescriptorWrite?.Invoke(this, new GattDescriptorEventArgs(gatt, descriptor, status));
        }


        public event EventHandler<MtuChangedEventArgs> MtuChanged;
        public override void OnMtuChanged(BluetoothGatt gatt, int mtu, GattStatus status)
        {
            base.OnMtuChanged(gatt, mtu, status);
            this.MtuChanged?.Invoke(this, new MtuChangedEventArgs(mtu, gatt, status));
        }


        public event EventHandler<GattRssiEventArgs> ReadRemoteRssi;
        public override void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, GattStatus status)
        {
            this.ReadRemoteRssi?.Invoke(this, new GattRssiEventArgs(gatt, rssi, status));
        }


        public event EventHandler<GattEventArgs> ReliableWriteCompleted;
        public override void OnReliableWriteCompleted(BluetoothGatt gatt, GattStatus status)
        {
            this.ReliableWriteCompleted?.Invoke(this, new GattEventArgs(gatt, status));
        }


        public event EventHandler<GattEventArgs> ServicesDiscovered;
        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            this.ServicesDiscovered?.Invoke(this, new GattEventArgs(gatt, status));
        }


        public event EventHandler<ConnectionStateEventArgs> ConnectionStateChanged;
        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            // TODO: fire only on success?
            this.ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs(gatt, status, newState));
        }
    }
}