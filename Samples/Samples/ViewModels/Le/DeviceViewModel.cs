using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using Plugin.BluetoothLE;
using Acr.UserDialogs;
using ReactiveUI;
using Samples.Services;
using Xamarin.Forms;


namespace Samples.ViewModels.Le
{

    public class DeviceViewModel : AbstractRootViewModel
    {
        readonly IList<IDisposable> cleanup = new List<IDisposable>();
        IDevice device;


        public DeviceViewModel(ICoreServices services) : base(services)
        {
            this.SelectCharacteristic = ReactiveCommand.Create<GattCharacteristicViewModel>(x => x.Select());
            this.SelectDescriptor = ReactiveCommand.Create<GattDescriptorViewModel>(x => x.Select());

            this.ConnectionToggle = ReactiveCommand.CreateFromTask(async x =>
            {
                try
                {
                    // don't cleanup connection - force user to d/c
                    if (this.device.Status == ConnectionStatus.Disconnected)
                    {
                        using (var cancelSrc = new CancellationTokenSource())
                        {
                            using (this.Dialogs.Loading("Connecting", cancelSrc.Cancel, "Cancel"))
                            {
                                await this.device.Connect().ToTask(cancelSrc.Token);
                            }
                        }
                    }
                    else
                    {
                        this.device.CancelConnection();
                    }
                }
                catch (Exception ex)
                {
                    this.Dialogs.Alert(ex.ToString());
                }
            });
            this.PairToDevice = ReactiveCommand.CreateFromTask(async x =>
            {
                if (!this.device.Features.HasFlag(DeviceFeatures.PairingRequests))
                {
                    this.Dialogs.Alert("Pairing is not supported on this platform");
                }
                else if (this.device.PairingStatus == PairingStatus.Paired)
                {
                    this.Dialogs.Alert("Device is already paired");
                }
                else
                {
                    await this.device.PairingRequest();
                }
            });
            this.RequestMtu = ReactiveCommand.CreateFromTask(
                async x =>
                {
                    if (!this.device.Features.HasFlag(DeviceFeatures.MtuRequests))
                    {
                        this.Dialogs.Alert("MTU Request not supported on this platform");
                    }
                    else
                    {
                        var result = await this.Dialogs.PromptAsync(new PromptConfig()
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
                            this.Dialogs.Alert("MTU Changed to " + actual);
                        }
                    }
                },
                this.WhenAny(
                    x => x.Status,
                    x => x.Value == ConnectionStatus.Connected
                )
            );
        }


        public override void Init(object args)
        {
            this.device = (IDevice)args;
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.Name = this.device.Name;
            this.Uuid = this.device.Uuid;

            this.cleanup.Add(this.device
                .WhenNameUpdated()
                .Subscribe(x => this.Name = this.device.Name)
            );

            this.cleanup.Add(this.device
                .WhenStatusChanged()
                .Subscribe (x => Device.BeginInvokeOnMainThread(() =>
                {
                    this.Status = x;

                    switch (x)
                    {
                        case ConnectionStatus.Disconnecting:
                        case ConnectionStatus.Connecting:
                            this.ConnectText = x.ToString();
                            break;

                        case ConnectionStatus.Disconnected:
                            this.ConnectText = "Connect";
                            this.GattCharacteristics.Clear();
                            this.GattDescriptors.Clear();
                            this.Rssi = 0;
                            break;

                        case ConnectionStatus.Connected:
                            this.ConnectText = "Disconnect";
                            //this.cleanup.Add(this.device
                            //    .WhenRssiUpdated()
                            //    .Subscribe(rssi => this.Rssi = rssi)
                            //);
                            break;
                    }
                }))
            );

            this.cleanup.Add(this.device
                .WhenMtuChanged()
                .Skip(1)
                .Subscribe(x => this.Dialogs.Alert($"MTU Changed size to {x}"))
            );

            this.cleanup.Add(this.device
                .WhenServiceDiscovered()
                .Subscribe(service =>
                {
                    var group = new Group<GattCharacteristicViewModel>(service.Uuid.ToString());
                    var characters = service
                        .WhenCharacteristicDiscovered()
                        .Subscribe(character =>
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                var vm = new GattCharacteristicViewModel(this.Dialogs, character);
                                group.Add(vm);
                                if (group.Count == 1)
                                    this.GattCharacteristics.Add(group);
                            });
                            character
                                .WhenDescriptorDiscovered()
                                .Subscribe(desc => Device.BeginInvokeOnMainThread(() =>
                                {
                                    var dvm = new GattDescriptorViewModel(this.Dialogs, desc);
                                    this.GattDescriptors.Add(dvm);
                                }));
                        });
                })
            );
        }


        public override void OnDeactivate()
        {
            base.OnDeactivate();
            foreach (var item in this.cleanup)
                item.Dispose();
        }


        public ICommand ConnectionToggle { get; }
        public ICommand PairToDevice { get; }
        public ICommand RequestMtu { get; }
        public ICommand SelectCharacteristic { get; }
        public ICommand SelectDescriptor { get; }


        string name;
        public string Name
        {
            get => this.name;
            private set => this.RaiseAndSetIfChanged(ref this.name, value);
        }


        string connectText = "Connect";
        public string ConnectText
        {
            get => this.connectText;
            private set => this.RaiseAndSetIfChanged(ref this.connectText, value);
        }


        Guid uuid;
        public Guid Uuid
        {
            get => this.uuid;
            private set => this.RaiseAndSetIfChanged(ref this.uuid, value);
        }


        int rssi;
        public int Rssi
        {
            get => this.rssi;
            private set => this.RaiseAndSetIfChanged(ref this.rssi, value);
        }


        ConnectionStatus status = ConnectionStatus.Disconnected;
        public ConnectionStatus Status
        {
            get => this.status;
            private set => this.RaiseAndSetIfChanged(ref this.status, value);
        }


        public ObservableCollection<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableCollection<Group<GattCharacteristicViewModel>>();
        public ObservableCollection<GattDescriptorViewModel> GattDescriptors { get; } = new ObservableCollection<GattDescriptorViewModel>();
    }
}
