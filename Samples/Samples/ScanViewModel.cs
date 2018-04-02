using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Ble
{
    public class ScanViewModel : ViewModel
    {
        readonly IUserDialogs dialogs;
        readonly IAdapter adapter;
        IDisposable scan;


        public ScanViewModel()
        {
            this.dialogs = UserDialogs.Instance;
            this.adapter = CrossBleAdapter.Current;

            this.Devices = new ObservableCollection<ScanResultViewModel>();

            this.adapter
                .WhenDeviceStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    var vm = this.Devices.FirstOrDefault(dev => dev.Uuid.Equals(x.Uuid));
                    if (vm != null)
                        vm.IsConnected = x.Status == ConnectionStatus.Connected;
                });

            this.adapter
                .WhenStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    this.IsSupported = x == AdapterStatus.PoweredOn;
                    this.Title = $"BLE Scanner ({x})";
                });

            this.SelectDevice = ReactiveCommand.CreateFromTask<ScanResultViewModel>(async x =>
            {
                this.scan?.Dispose();
                await App.Current.MainPage.Navigation.PushAsync(new DevicePage
                {
                    BindingContext = new DeviceViewModel(x.Device)
                });
            });

            this.OpenSettings = ReactiveCommand.Create(() =>
            {
                if (this.adapter.Features.HasFlag(AdapterFeatures.OpenSettings))
                {
                    this.adapter.OpenSettings();
                }
                else
                {
                    this.dialogs.Alert("Cannot open bluetooth settings");
                }
            });

            this.ToggleAdapterState = ReactiveCommand.Create(
                () =>
                {
                    if (this.adapter.CanControlAdapterState())
                    {
                        this.adapter.SetAdapterState(true);
                    }
                    else
                    {
                        this.dialogs.Alert("Cannot change bluetooth adapter state");
                    }
                },
                this.adapter
                    .WhenStatusChanged()
                    .ObserveOn(RxApp.MainThreadScheduler)
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

                        this.scan = CrossBleAdapter
                            .Current
                            .Scan()
                            .Buffer(TimeSpan.FromSeconds(1))
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(results =>
                            {
                                foreach (var result in results)
                                    this.OnScanResult(result);
                            });
                    }
                },
                this.WhenAny(
                    x => x.IsSupported,
                    x => x.Value
                )
            );
        }


        public override void OnDeactivated()
        {
            base.OnDeactivated();
            this.scan?.Dispose();
        }


        public ICommand ScanToggle { get; }
        public ICommand OpenSettings { get; }
        public ICommand ToggleAdapterState { get; }
        public ICommand SelectDevice { get; }
        public ObservableCollection<ScanResultViewModel> Devices { get; }


        bool scanning;
        public bool IsScanning
        {
            get => this.scanning;
            private set => this.RaiseAndSetIfChanged(ref this.scanning, value);
        }


        bool supported;
        public bool IsSupported
        {
            get => this.supported;
            private set => this.RaiseAndSetIfChanged(ref this.supported, value);
        }


        string scanText;
        public string ScanText
        {
            get => this.scanText;
            private set => this.RaiseAndSetIfChanged(ref this.scanText, value);
        }


        string title;
        public string Title
        {
            get => this.title;
            private set => this.RaiseAndSetIfChanged(ref this.title, value);
        }


        void OnScanResult(IScanResult result)
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
        }
    }
}
