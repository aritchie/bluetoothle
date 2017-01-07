using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr;
using Acr.Ble;
using ReactiveUI;
using Samples.Services;


namespace Samples.ViewModels.Le
{
    public class ConnectDevicesViewModel : AbstractRootViewModel
    {
        IDisposable deviceStateSub;


        public ConnectDevicesViewModel(ICoreServices services) : base(services)
        {
            this.Select = new Command<IDevice>(dev => 
                this.VmManager.Push<DeviceViewModel>(dev)
            );
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.DeviceList.AddRange(this.BleAdapter.GetConnectedDevices());
            this.deviceStateSub = this.BleAdapter
                .WhenDeviceStatusChanged()
                .Where(x => x.Status == ConnectionStatus.Connected)
                .Subscribe(this.DeviceList.Add);
        }


        public override void OnDeactivate()
        {
            base.OnDeactivate();
            this.deviceStateSub.Dispose();
        }


        public ICommand Select { get; }
        public IReactiveList<IDevice> DeviceList { get; } = new ReactiveList<IDevice>();
    }
}
