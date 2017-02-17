using System;


namespace Acr.Ble
{
    [Flags]
    public enum DeviceFeatures
    {
        None = 0,
        PairingRequests = 1,
        MtuRequests = 2,
        ReliableTransactions = 4,

        All = PairingRequests | MtuRequests | ReliableTransactions
    }
}
