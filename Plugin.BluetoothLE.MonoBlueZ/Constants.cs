using System;
using DBus;


namespace Plugin.BluetoothLE
{
    public static class Constants
    {
        public static readonly ObjectPath AgentPath = new ObjectPath("/agent");
        public const string SERVICE = "org.bluez";
    }
}
