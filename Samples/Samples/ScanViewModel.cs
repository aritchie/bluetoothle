using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.Collections;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;


namespace Samples
{
    public class ScanViewModel : ViewModel
    {
        IAdapter adapter;
        IDisposable scan;


        public ScanViewModel(INavigationService navigationService, IUserDialogs dialogs)
        {
            this.SelectDevice = ReactiveCommand.CreateFromTask<ScanResultViewModel>(
                x => navigationService.NavToDevice(x.Device)
            );

            this.OpenSettings = ReactiveCommand.Create(() =>
            {
                if (this.adapter.Features.HasFlag(AdapterFeatures.OpenSettings))
                {
                    this.adapter.OpenSettings();
                }
                else
                {
                    dialogs.Alert("Cannot open bluetooth settings");
                }
            });

            this.ToggleAdapterState = ReactiveCommand.Create(
                () =>
                {
                    if (this.adapter.CanControlAdapterState())
                    {
                        var poweredOn = this.adapter.Status == AdapterStatus.PoweredOn;
                        this.adapter.SetAdapterState(!poweredOn);
                    }
                    else
                    {
                        dialogs.Alert("Cannot change bluetooth adapter state");
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
                        this.scan = this
                            .adapter
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
                                ex => dialogs.Alert(ex.ToString(), "ERROR")
                            )
                            .DisposeWith(this.DeactivateWith);
                    }
                }
            );
        }


        public override void OnNavigatingTo(NavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            this.adapter = parameters.GetValue<IAdapter>("adapter");
            this.Title = $"{this.adapter.DeviceName} ({this.adapter.Status})";
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.IsScanning = false;
        }


        public ICommand ScanToggle { get; }
        public ICommand OpenSettings { get; }
        public ICommand ToggleAdapterState { get; }
        public ICommand SelectDevice { get; }
        public ObservableList<ScanResultViewModel> Devices { get; } = new ObservableList<ScanResultViewModel>();


        [Reactive] public string Title { get; private set; }
        [Reactive] public bool IsScanning { get; private set; }
    }
}
