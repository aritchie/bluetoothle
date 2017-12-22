using System;
using System.Collections.Generic;
using System.Text;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Platforms.Apple.Server
{
    public class BleCbPerhiperhalManagerDelegate : CBPeripheralManagerDelegate
    {
        public override void StateUpdated(CBPeripheralManager peripheral)
        {
            throw new NotImplementedException();
        }
    }
}
