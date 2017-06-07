using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Services;
using Xamarin.Forms;


namespace Samples.ViewModels.Le
{
    public class ConnectedDevicesViewModel : AbstractRootViewModel
    {
        IDisposable deviceStateSub;


        public ConnectedDevicesViewModel(ICoreServices services) : base(services)
        {
            this.SelectDevice = new Command<IDevice>(dev =>
                this.VmManager.Push<DeviceViewModel>(dev)
            );
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.DeviceList = this.BleAdapter.GetConnectedDevices().ToList();

            this.deviceStateSub = this.BleAdapter
                .WhenDeviceStatusChanged()
                .Subscribe(x => Device.BeginInvokeOnMainThread(() =>
                {
                    switch (x.Status)
                    {
                        case ConnectionStatus.Disconnected:
                            this.DeviceList.Remove(x);
                            this.RaisePropertyChanged(nameof(DeviceList));
                            break;

                        case ConnectionStatus.Connected:
                            this.DeviceList.Add(x);
                            this.RaisePropertyChanged(nameof(DeviceList));
                            break;
                    }
            }));
        }


        public override void OnDeactivate()
        {
            base.OnDeactivate();
            this.deviceStateSub.Dispose();
        }


        public ICommand SelectDevice { get; }


        IList<IDevice> devices;

        public IList<IDevice> DeviceList
        {
            get => this.devices;
            private set
            {
                this.devices = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
