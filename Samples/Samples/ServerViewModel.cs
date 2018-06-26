using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using Plugin.BluetoothLE.Server;
using ReactiveUI;
using Samples.Infrastructure;
using Device = Xamarin.Forms.Device;


namespace Samples.Ble
{
    public class ServerViewModel : ViewModel
    {
        readonly IAdapter adapter;
        readonly IUserDialogs dialogs;
        IDisposable timer;
        IGattServer server;


        public ServerViewModel()
        {
            this.adapter = CrossBleAdapter.Current;
            this.dialogs = UserDialogs.Instance;

            this.adapter
                .WhenStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.Status = x);

            this.ToggleServer = ReactiveCommand.Create(() =>
            {
                if (this.adapter.Status != AdapterStatus.PoweredOn)
                {
                    this.dialogs.Alert("Could not start GATT Server.  Adapter Status: " + this.adapter.Status);
                    return;
                }

                if (!this.adapter.Features.HasFlag(AdapterFeatures.ServerGatt))
                {
                    this.dialogs.Alert("GATT Server is not supported on this platform configuration");
                    return;
                }

                if (this.server == null)
                {
                    this.BuildServer();
                    this.adapter.Advertiser.Start(new AdvertisementData
                    {
                        LocalName = "My GATT"
                        //ManufacturerData = new ManufacturerData()
                    });
                }
                else
                {
                    this.ServerText = "Start Server";
                    this.adapter.Advertiser.Stop();
                    this.OnEvent("GATT Server Stopped");
                    this.server.Dispose();
                    this.server = null;
                    this.timer?.Dispose();
                }
            });

            this.Clear = ReactiveCommand.Create(() => this.Output = String.Empty);
        }


        string serverText = "Start Server";
        public string ServerText
        {
            get => this.serverText;
            set => this.RaiseAndSetIfChanged(ref this.serverText, value);
        }


        string chValue;
        public string CharacteristicValue
        {
            get => this.chValue;
            set => this.RaiseAndSetIfChanged(ref this.chValue, value);
        }


        string descValue;
        public string DescriptorValue
        {
            get => this.descValue;
            set => this.RaiseAndSetIfChanged(ref this.descValue, value);
        }


        string output;
        public string Output
        {
            get => this.output;
            private set => this.RaiseAndSetIfChanged(ref this.output, value);
        }


        AdapterStatus status;
        public AdapterStatus Status
        {
            get => this.status;
            set => this.RaiseAndSetIfChanged(ref this.status, value);
        }


        public ICommand ToggleServer { get; }
        public ICommand Clear { get; }


        void BuildServer()
        {
            try
            {
                this.OnEvent("GATT Server Starting");
                this.server = this.adapter.CreateGattServer();

                var counter = 0;
                var service = this.server.CreateService(Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE"), true);
                this.BuildCharacteristics(service, Guid.Parse("A495FF21-C5B1-4B44-B512-1370F02D74DE")); // scratch #1
                this.BuildCharacteristics(service, Guid.Parse("A495FF22-C5B1-4B44-B512-1370F02D74DE")); // scratch #2
                this.BuildCharacteristics(service, Guid.Parse("A495FF23-C5B1-4B44-B512-1370F02D74DE")); // scratch #3
                this.BuildCharacteristics(service, Guid.Parse("A495FF24-C5B1-4B44-B512-1370F02D74DE")); // scratch #4
                this.BuildCharacteristics(service, Guid.Parse("A495FF25-C5B1-4B44-B512-1370F02D74DE")); // scratch #5
                this.server.AddService(service);
                this.ServerText = "Stop Server";

                this.timer = Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .Select(_ => Observable.FromAsync(async ct =>
                    {
                        var subscribed = service.Characteristics.Where(x => x.SubscribedDevices.Count > 0);
                        foreach (var ch in subscribed)
                        {
                            counter++;
                            await ch.BroadcastObserve(Encoding.UTF8.GetBytes(counter.ToString()));
                        }
                    }))
                    .Merge(5)
                    .Subscribe();

                this.server
                    .WhenAnyCharacteristicSubscriptionChanged()
                    .Subscribe(x =>
                        this.OnEvent($"[WhenAnyCharacteristicSubscriptionChanged] UUID: {x.Characteristic.Uuid} - Device: {x.Device.Uuid} - Subscription: {x.IsSubscribing}")
                    );

                //descriptor.WhenReadReceived().Subscribe(x =>
                //    this.OnEvent("Descriptor Read Received")
                //);
                //descriptor.WhenWriteReceived().Subscribe(x =>
                //{
                //    var write = Encoding.UTF8.GetString(x.Value, 0, x.Value.Length);
                //    this.OnEvent($"Descriptor Write Received - {write}");
                //});
                this.OnEvent("GATT Server Started");
            }
            catch (Exception ex)
            {
                this.dialogs.Alert("Error building gatt server - " + ex);
            }
        }


        void BuildCharacteristics(Plugin.BluetoothLE.Server.IGattService service, Guid characteristicId)
        {
            var characteristic = service.AddCharacteristic(
                characteristicId,
                CharacteristicProperties.Notify | CharacteristicProperties.Read | CharacteristicProperties.Write | CharacteristicProperties.WriteNoResponse,
                GattPermissions.Read | GattPermissions.Write
            );

            characteristic
                .WhenDeviceSubscriptionChanged()
                .Subscribe(e =>
                {
                    var @event = e.IsSubscribed ? "Subscribed" : "Unsubcribed";
                    this.OnEvent($"Device {e.Device.Uuid} {@event}");
                    this.OnEvent($"Charcteristic Subcribers: {characteristic.SubscribedDevices.Count}");
                });

            characteristic.WhenReadReceived().Subscribe(x =>
            {
                var write = this.CharacteristicValue;
                if (String.IsNullOrWhiteSpace(write))
                    write = "(NOTHING)";

                x.Value = Encoding.UTF8.GetBytes(write);
                this.OnEvent("Characteristic Read Received");
            });
            characteristic.WhenWriteReceived().Subscribe(x =>
            {
                var write = Encoding.UTF8.GetString(x.Value, 0, x.Value.Length);
                this.OnEvent($"Characteristic Write Received - {write}");
            });
        }


        void OnEvent(string msg) => Device.BeginInvokeOnMainThread(() =>
            this.Output += msg + Environment.NewLine + Environment.NewLine
        );
    }
}
