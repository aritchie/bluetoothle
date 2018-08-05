using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.Collections;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Ble
{
    public class ScanViewModel : ViewModel
    {
        IDisposable scan;


        public ScanViewModel()
        {
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
                if (CrossBleAdapter.Current.Features.HasFlag(AdapterFeatures.OpenSettings))
                {
                    CrossBleAdapter.Current.OpenSettings();
                }
                else
                {
                    UserDialogs.Instance.Alert("Cannot open bluetooth settings");
                }
            });

            this.ToggleAdapterState = ReactiveCommand.Create(
                () =>
                {
                    if (CrossBleAdapter.Current.CanControlAdapterState())
                    {
                        var poweredOn = CrossBleAdapter.Current.Status == AdapterStatus.PoweredOn;
                        CrossBleAdapter.Current.SetAdapterState(!poweredOn);
                    }
                    else
                    {
                        UserDialogs.Instance.Alert("Cannot change bluetooth adapter state");
                    }
                }
            );

            this.ScanToggle = ReactiveCommand.Create(
                () =>
                {
                    if (this.IsScanning)
                    {
                        this.scan?.Dispose();
                        this.IsScanning = false;
                    }
                    else
                    {
                        this.Devices.Clear();

                        this.IsScanning = true;
                        this.scan = CrossBleAdapter
                            .Current
                            .Scan()
                            .Buffer(TimeSpan.FromSeconds(1))
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(
                                results =>
                                {
                                    var list = new List<ScanResultViewModel>();
                                    foreach (var result in results)
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
                                            list.Add(dev);
                                        }
                                    }
                                    if (list.Any())
                                        this.Devices.AddRange(list);
                                },
                                ex => UserDialogs.Instance.Alert(ex.ToString(), "ERROR")
                            )
                            .DisposeWith(this.DeactivateWith);
                    }
                }
            );
        }


        public override void OnActivated()
        {
            base.OnActivated();
            CrossBleAdapter
                .Current
                .WhenStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(this.Title)))
                .DisposeWith(this.DeactivateWith);

        }


        public ICommand ScanToggle { get; }
        public ICommand OpenSettings { get; }
        public ICommand ToggleAdapterState { get; }
        public ICommand SelectDevice { get; }
        public ObservableList<ScanResultViewModel> Devices { get; } = new ObservableList<ScanResultViewModel>();


        public string Title => $"{CrossBleAdapter.Current.DeviceName} ({CrossBleAdapter.Current.Status})";


        bool scanning;
        public bool IsScanning
        {
            get => this.scanning;
            private set => this.RaiseAndSetIfChanged(ref this.scanning, value);
        }
    }
}
