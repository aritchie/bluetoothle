using System;


namespace Acr.Ble
{
    public enum TransactionStatus
    {
        Active,
        Committing,
        Committed,
        Aborted
    }
}
