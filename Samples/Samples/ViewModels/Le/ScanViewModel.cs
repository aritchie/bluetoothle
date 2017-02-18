using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.BluetoothLE;
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

            this.AppState.WhenBackgrounding().Subscribe(_ => this.scan?.Dispose());
            this.BleAdapter.WhenScanningStatusChanged().Subscribe(on =>
            {
                this.IsScanning = on;
                this.ScanText = on ? "Stop Scan" : "Scan";
            });
            this.Devices = new ObservableCollection<ScanResultViewModel>();

            this.SelectDevice = ReactiveCommand.Create<ScanResultViewModel>(x =>
            {
                this.scan?.Dispose();
                services.VmManager.Push<DeviceViewModel>(x.Device);
            });

            this.OpenSettings = ReactiveCommand.Create(() =>
            {
                if (this.BleAdapter.Features.HasFlag(AdapterFeatures.OpenSettings))
                {
                    this.BleAdapter.OpenSettings();
                }
                else
                {
                    this.Dialogs.Alert("Cannot open bluetooth settings");
                }
            });
            this.ToggleAdapterState = ReactiveCommand.Create(
                () =>
                {
                    if (this.BleAdapter.CanControlAdapterState())
                    {
                        this.BleAdapter.SetAdapterState(true);
                    }
                    else
                    {
                        this.Dialogs.Alert("Cannot change bluetooth adapter state");
                    }
                },
                this.BleAdapter
                    .WhenStatusChanged()
                    .Select(x => x == AdapterStatus.PoweredOff)
            );

            this.ScanToggle = ReactiveCommand.Create(
                () =>
                {
                    if (this.IsScanning)
                    {
                        this.scan?.Dispose();
                    }
                    else
                    {
                        this.Devices.Clear();
                        this.ScanText = "Stop Scan";

                        this.scan = this.BleAdapter
                            .Scan()
                            .Subscribe(this.OnScanResult);
                    }
                },
                this.WhenAny(
                    x => x.IsSupported,
                    x => x.Value
                )
            );
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.BleAdapter
                .WhenStatusChanged()
                .Subscribe(x => Device.BeginInvokeOnMainThread(() =>
                {
                    this.IsSupported = x == AdapterStatus.PoweredOn;
                    this.Title = $"BLE Scanner ({x})";
                }));
        }


        public ICommand ScanToggle { get; }
        public ICommand OpenSettings { get; }
        public ICommand ToggleAdapterState { get; }
        public ICommand SelectDevice { get; }
        public ObservableCollection<ScanResultViewModel> Devices { get; }
        [Reactive] public bool IsScanning { get; private set; }
        [Reactive] public bool IsSupported { get; private set; }
        [Reactive] public string ScanText { get; private set; }
        [Reactive] public string Title { get; private set; }


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
