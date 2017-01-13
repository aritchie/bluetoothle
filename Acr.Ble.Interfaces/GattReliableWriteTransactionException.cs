using System;


namespace Acr.Ble
{
    public class GattReliableWriteTransactionException : Exception
    {
        public GattReliableWriteTransactionException(string msg) : base(msg) { }
    }
}
