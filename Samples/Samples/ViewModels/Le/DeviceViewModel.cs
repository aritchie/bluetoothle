using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.Ble;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Services;
using Xamarin.Forms;

namespace Samples.ViewModels.Le
{

    public class DeviceViewModel : AbstractRootViewModel
    {
        IDisposable conn;
        IDisposable readRssiTimer;
        IDevice device;


        public DeviceViewModel(ICoreServices services) : base(services)
        {
            this.SelectCharacteristic = new Acr.Command<GattCharacteristicViewModel>(x => x.Select());
            this.SelectDescriptor = new Acr.Command<GattDescriptorViewModel>(x => x.Select());

            this.ConnectionToggle = ReactiveCommand.CreateAsyncTask(
                this.WhenAny(
                    x => x.Status,
                    x => x.Value != ConnectionStatus.Disconnecting
                ),
                x =>
                {
                    if (this.conn == null)
                    {
                        this.conn = this.device.CreateConnection().Subscribe();
                    }
                    else 
                    {
                        this.conn?.Dispose();
                        this.conn = null;
                    }
                    return Task.FromResult(Unit.Default);
                }
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

            this.device
                .WhenNameUpdated()
                .Subscribe(x => this.Name = this.device.Name);

            this.device
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
                            this.readRssiTimer?.Dispose();
                            this.GattCharacteristics.Clear();
                            this.GattDescriptors.Clear();
                            break;

                        case ConnectionStatus.Connected:
                            this.ConnectText = "Disconnect";
                            this.readRssiTimer = this.device
                                .WhenRssiUpdated()
                                .Subscribe(rssi => this.Rssi = rssi);
                            break;
                    }
                }));

            this.device
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
                });
        }


        public override void OnDeactivate()
        {
            base.OnDeactivate();

            this.device.Disconnect();
            this.readRssiTimer?.Dispose();
            this.readRssiTimer = null;
            this.conn?.Dispose();
            this.conn = null;
        }


        public ICommand ConnectionToggle { get; }
        public Acr.Command<GattCharacteristicViewModel> SelectCharacteristic { get; }
        public Acr.Command<GattDescriptorViewModel> SelectDescriptor { get; }
        [Reactive] public string Name { get; private set; }
        [Reactive] public string ConnectText { get; private set; } = "Connect";
        [Reactive] public Guid Uuid { get; private set; }
        [Reactive] public int Rssi { get; private set; }
        [Reactive] public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
        public ObservableCollection<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableCollection<Group<GattCharacteristicViewModel>>();
        public ObservableCollection<GattDescriptorViewModel> GattDescriptors { get; } = new ObservableCollection<GattDescriptorViewModel>();
    }
}
