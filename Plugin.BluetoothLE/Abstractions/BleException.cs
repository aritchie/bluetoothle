using System;


namespace Plugin.BluetoothLE
{
    public class BleException : Exception
    {
        public BleException(string message) : base(message) { }
        public BleException(string message, Exception inner) : base(message, inner) { }
    }
}
