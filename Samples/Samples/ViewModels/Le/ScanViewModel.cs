using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.Ble;
using Acr;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Services;
using Xamarin.Forms;


namespace Samples.ViewModels.Le
{
    public class ScanViewModel : AbstractRootViewModel
    {
        IDisposable scan;
        IDisposable connect;


        public ScanViewModel(ICoreServices services) : base(services)
        {
            this.connect = this.BleAdapter
                .WhenDeviceStatusChanged()
                .Subscribe(x =>
                {
                    var vm = this.Devices.FirstOrDefault(dev => dev.Uuid.Equals(x.Uuid));
                    if (vm != null)
                        vm.IsConnected = x.Status == ConnectionStatus.Connected;
                });

            this.AppState.WhenBackgrounding().Subscribe(_ => this.StopScan());
            this.Devices = new ObservableCollection<ScanResultViewModel>();

            this.SelectDevice = new Acr.Command<ScanResultViewModel>(x =>
            {
                this.StopScan();
                services.VmManager.Push<DeviceViewModel>(x.Device);
            });

            this.ScanToggle = ReactiveCommand.CreateAsyncTask(
                this.WhenAny(
                    x => x.IsSupported,
                    x => x.Value
                ),
                x =>
                {
                    if (this.ScanText == "Scan")
                    {
                        this.Devices.Clear();
                        this.ScanText = "Stop Scan";

                        this.scan = this.BleAdapter
                            .Scan()
                            .Subscribe(
                                this.OnScanResult
                            );
                    }
                    else
                    {
                        this.StopScan();
                    }
                    return Task.FromResult<object>(null);
                }
            );
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.BleAdapter
                .WhenStatusChanged()
                .Subscribe(x =>
                {
                    this.IsSupported = x == AdapterStatus.PoweredOn;
                    this.Title = $"BLE Scanner ({x})";
                });
        }


        public ICommand ScanToggle { get; }
        public Acr.Command<ScanResultViewModel> SelectDevice { get; }
        public ObservableCollection<ScanResultViewModel> Devices { get; }
        [Reactive] public bool IsSupported { get; private set; }
        [Reactive] public string ScanText { get; private set; } = "Scan";
        [Reactive] public string Title { get; private set; }


        void StopScan()
        {
            this.ScanText = "Scan";
            this.scan?.Dispose();
        }


        void OnScanResult(IScanResult result)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var dev = this.Devices.FirstOrDefault(x => x.Uuid.Equals(result.Device.Uuid));
                if (dev != null)
                {
                    dev.TrySet(result);
                }
                else
                {
                    dev = new ScanResultViewModel();
                    dev.TrySet(result);
                    this.Devices.Add(dev);
                }
            });
        }
    }
}
