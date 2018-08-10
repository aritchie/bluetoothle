using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Ble
{
    public class DeviceViewModel : ViewModel
    {
        readonly IDevice device;


        public DeviceViewModel(IDevice device)
        {
            this.device = device;

            this.SelectCharacteristic = ReactiveCommand.Create<GattCharacteristicViewModel>(x => x.Select());

            this.ConnectionToggle = ReactiveCommand.Create(() =>
            {
                // don't cleanup connection - force user to d/c
                if (this.device.Status == ConnectionStatus.Disconnected)
                {
                    this.device.Connect();
                }
                else
                {
                    this.device.CancelConnection();
                }
            });

            this.PairToDevice = ReactiveCommand.Create(() =>
            {
                if (!this.device.Features.HasFlag(DeviceFeatures.PairingRequests))
                {
                    UserDialogs.Instance.Toast("Pairing is not supported on this platform");
                }
                else if (this.device.PairingStatus == PairingStatus.Paired)
                {
                    UserDialogs.Instance.Toast("Device is already paired");
                }
                else
                {
                    this.device
                        .PairingRequest()
                        .Subscribe(x =>
                        {
                            var txt = x ? "Device Paired Successfully" : "Device Pairing Failed";
                            UserDialogs.Instance.Toast(txt);
                            this.RaisePropertyChanged(nameof(this.PairingText));
                        });
                }
            });

            this.RequestMtu = ReactiveCommand.CreateFromTask(
                async x =>
                {
                    if (!this.device.Features.HasFlag(DeviceFeatures.MtuRequests))
                    {
                        UserDialogs.Instance.Alert("MTU Request not supported on this platform");
                    }
                    else
                    {
                        var result = await UserDialogs.Instance.PromptAsync(new PromptConfig()
                            .SetTitle("MTU Request")
                            .SetMessage("Range 20-512")
                            .SetInputMode(InputType.Number)
                            .SetOnTextChanged(args =>
                            {
                                var len = args.Value?.Length ?? 0;
                                if (len > 0)
                                {
                                    if (len > 3)
                                    {
                                        args.Value = args.Value.Substring(0, 3);
                                    }
                                    else
                                    {
                                        var value = Int32.Parse(args.Value);
                                        args.IsValid = value >= 20 && value <= 512;
                                    }
                                }
                            })
                        );
                        if (result.Ok)
                        {
                            var actual = await this.device.RequestMtu(Int32.Parse(result.Text));
                            UserDialogs.Instance.Toast("MTU Changed to " + actual);
                        }
                    }
                },
                this.WhenAny(
                    x => x.ConnectText,
                    x => x.GetValue().Equals("Disconnect")
                )
            );
        }


        public override void OnActivated()
        {
            base.OnActivated();

            this.device
                .WhenReadRssiContinuously(TimeSpan.FromSeconds(3))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.Rssi = x)
                .DisposeWith(this.DeactivateWith);

            this.device
                .WhenStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    switch (status)
                    {
                        case ConnectionStatus.Connecting:
                            this.ConnectText = "Cancel Connection";
                            break;

                        case ConnectionStatus.Connected:
                            this.ConnectText = "Disconnect";
                            break;

                        case ConnectionStatus.Disconnected:
                            this.ConnectText = "Connect";
                            try
                            {
                                this.GattCharacteristics.Clear();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }

                            break;
                    }
                })
                .DisposeWith(this.DeactivateWith);

            this.device
                .WhenAnyCharacteristicDiscovered()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(chs =>
                {
                    try
                    {
                        var service = this.GattCharacteristics.FirstOrDefault(x => x.ShortName.Equals(chs.Service.Uuid.ToString()));
                        if (service == null)
                        {
                            service = new Group<GattCharacteristicViewModel>(
                                $"{chs.Service.Description} ({chs.Service.Uuid})",
                                chs.Service.Uuid.ToString()
                            );
                            this.GattCharacteristics.Add(service);
                        }

                        service.Add(new GattCharacteristicViewModel(chs));
                    }
                    catch (Exception ex)
                    {
                        // eat it
                        Console.WriteLine(ex);
                    }
                })
                .DisposeWith(this.DeactivateWith);
        }


        public ICommand ConnectionToggle { get; }
        public ICommand PairToDevice { get; }
        public ICommand RequestMtu { get; }
        public ICommand SelectCharacteristic { get; }

        public string Name => this.device.Name ?? "Unknown";
        public Guid Uuid => this.device.Uuid;
        public string PairingText => this.device.PairingStatus == PairingStatus.Paired ? "Device Paired" : "Pair Device";
        public ObservableCollection<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableCollection<Group<GattCharacteristicViewModel>>();


        string connectText = "Connect";
        public string ConnectText
        {
            get => this.connectText;
            private set => this.RaiseAndSetIfChanged(ref this.connectText, value);
        }


        int rssi;
        public int Rssi
        {
            get => this.rssi;
            private set => this.RaiseAndSetIfChanged(ref this.rssi, value);
        }
    }
}
