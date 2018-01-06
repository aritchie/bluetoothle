using System;


namespace Plugin.BluetoothLE
{
    public class BleException : Exception
    {
        public BleException(string message) : base(message) { }
    }
}
