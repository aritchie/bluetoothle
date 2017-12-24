using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Plugin.BluetoothLE;
using Plugin.BluetoothLE.Server;
using ReactiveUI;
using Samples.Services;
using Device = Xamarin.Forms.Device;


namespace Samples.ViewModels.Le
{
    public class ServerViewModel : AbstractRootViewModel
    {
        IDisposable notifyBroadcast;


        public ServerViewModel(ICoreServices services) : base(services)
        {
            this.BleAdapter
                .WhenStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.Status = x);

            this.ToggleServer = ReactiveCommand.CreateFromTask(async _ =>
            {
                if (this.BleAdapter.Status != AdapterStatus.PoweredOn)
                {
                    this.Dialogs.Alert("Could not start GATT Server.  Adapter Status: " + this.BleAdapter.Status);
                    return;
                }

                try
                {
                    this.BuildServer();
                    if (this.BleAdapter.GattServer.IsRunning)
                    {
                        this.BleAdapter.GattServer.Stop();
                        this.BleAdapter.Advertiser.Stop();
                    }
                    else
                    {
                        await this.BleAdapter.GattServer.Start();
                        this.BleAdapter.Advertiser.Start(new AdvertisementData
                        {
                            LocalName = "TestServer"
                        });
                    }
                }

                catch (Exception ex)
                {
                    this.Dialogs.Alert(ex.ToString(), "ERROR");
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
                var service = this.BleAdapter.GattServer.AddService(Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE"), true);
                this.BuildCharacteristics(service, Guid.Parse("A495FF21-C5B1-4B44-B512-1370F02D74DE")); // scratch #1
                this.BuildCharacteristics(service, Guid.Parse("A495FF22-C5B1-4B44-B512-1370F02D74DE")); // scratch #2
                this.BuildCharacteristics(service, Guid.Parse("A495FF23-C5B1-4B44-B512-1370F02D74DE")); // scratch #3
                this.BuildCharacteristics(service, Guid.Parse("A495FF24-C5B1-4B44-B512-1370F02D74DE")); // scratch #4
                this.BuildCharacteristics(service, Guid.Parse("A495FF25-C5B1-4B44-B512-1370F02D74DE")); // scratch #5

                this.BleAdapter
                    .GattServer
                    .WhenRunningChanged()
                    .Catch<bool, ArgumentException>(ex =>
                    {
                        this.Dialogs.Alert("Error Starting GATT Server - " + ex);
                        return Observable.Return(false);
                    })
                    .Subscribe(started => Device.BeginInvokeOnMainThread(() =>
                    {
                        if (!started)
                        {
                            this.ServerText = "Start Server";
                            this.OnEvent("GATT Server Stopped");
                        }
                        else
                        {
                            this.notifyBroadcast?.Dispose();
                            this.notifyBroadcast = null;

                            this.ServerText = "Stop Server";
                            this.OnEvent("GATT Server Started");
                            foreach (var s in this.BleAdapter.GattServer.Services)
                            {
                                this.OnEvent($"Service {s.Uuid} Created");
                                foreach (var ch in s.Characteristics)
                                {
                                    this.OnEvent($"Characteristic {ch.Uuid} Online - Properties {ch.Properties}");
                                }
                            }
                        }
                    }));

                this.BleAdapter
                    .GattServer
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
            }
            catch (Exception ex)
            {
                this.Dialogs.Alert("Error building gatt server - " + ex);
            }
        }


        void BuildCharacteristics(Plugin.BluetoothLE.Server.IGattService service, Guid characteristicId)
        {
            var characteristic = service.AddCharacteristic(
                characteristicId,
                CharacteristicProperties.Notify | CharacteristicProperties.Read | CharacteristicProperties.Write | CharacteristicProperties.WriteNoResponse,
                GattPermissions.Read | GattPermissions.Write
            );
            characteristic.WhenDeviceSubscriptionChanged().Subscribe(e =>
            {
                var @event = e.IsSubscribed ? "Subscribed" : "Unsubcribed";
                this.OnEvent($"Device {e.Device.Uuid} {@event}");
                this.OnEvent($"Charcteristic Subcribers: {characteristic.SubscribedDevices.Count}");

                if (this.notifyBroadcast == null)
                {
                    this.OnEvent("Starting Subscriber Thread");
                    this.notifyBroadcast = Observable
                        .Interval(TimeSpan.FromSeconds(1))
                        .Where(x => characteristic.SubscribedDevices.Count > 0)
                        .Subscribe(_ =>
                        {
                            try
                            {
                                var dt = DateTime.Now.ToString("g");
                                var bytes = Encoding.UTF8.GetBytes(dt);
                                characteristic
                                    .BroadcastObserve(bytes)
                                    .Subscribe(x =>
                                    {
                                        var state = x.Success ? "Successfully" : "Failed";
                                        var data = Encoding.UTF8.GetString(x.Data, 0, x.Data.Length);
                                        this.OnEvent($"{state} Broadcast {data} to device {x.Device.Uuid} from characteristic {x.Characteristic}");
                                    });
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error during broadcast: " + ex);
                            }
                        });
                }
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
