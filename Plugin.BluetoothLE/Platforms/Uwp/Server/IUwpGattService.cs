using System;
using System.Threading.Tasks;


namespace Plugin.BluetoothLE.Server
{
    public interface IUwpGattService : IGattService
    {
        Task Init();
        void Stop();
    }
}
