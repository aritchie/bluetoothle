using System;


namespace Plugin.BluetoothLE
{
    public class GattReliableWriteTransactionException : Exception
    {
        public GattReliableWriteTransactionException(string msg) : base(msg) { }
    }
}
