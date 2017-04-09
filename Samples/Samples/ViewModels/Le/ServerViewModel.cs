//using System;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Reactive.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Windows.Input;
//using Acr.UserDialogs;
//using Device = Xamarin.Forms.Device;
//using Command = Xamarin.Forms.Command;


//namespace Sample.ViewModels.Le
//{
//    [ImplementPropertyChanged]
//    public class MainViewModel : INotifyPropertyChanged
//    {
//        readonly IBleAdapter adapter;
//        readonly IUserDialogs dialogs;
//        IGattServer server;
//        IDisposable notifyBroadcast;


//        public MainViewModel()
//        {
//            this.adapter = BleAdapter.Current;
//            this.dialogs = UserDialogs.Instance;
//            this.adapter
//                .WhenStatusChanged()
//                .Subscribe(x => this.Status = x);

//            var cmd = new Command(async _ =>
//            {
//                if (this.adapter.Status != AdapterStatus.PoweredOn)
//                {
//                    this.dialogs.Alert("Could not start GATT Server.  Adapter Status: " + this.adapter.Status);
//                    return;
//                }

//                try
//                {
//                    this.BuildServer();
//                    if (this.server.IsRunning)
//                    {
//                        this.server.Stop();
//                    }
//                    else
//                    {
//                        await this.server.Start(new AdvertisementData
//                        {
//                            LocalName = "TestServer"
//                        });
//                    }
//                }

//                catch (Exception ex)
//                {
//                    this.dialogs.Alert(ex.ToString(), "ERROR");
//                }
//            });

//            this.ToggleServer = cmd;
//            this.Clear = new Command(() => this.Output = String.Empty);
//        }


//        public string ServerText { get; set; }
//        public string CharacteristicValue { get; set; }
//        public string DescriptorValue { get; set; }
//        public string Output { get; private set; }
//        public AdapterStatus Status { get; set; }
//        public ICommand ToggleServer { get; }
//        public ICommand Clear { get; }


//        void BuildServer()
//        {
//            if (this.server != null)
//                return;

//            try
//            {
//                this.server = this.adapter.CreateGattServer();
//                var service = this.server.AddService(Guid.NewGuid(), true);

//                var characteristic = service.AddCharacteristic(
//                    Guid.NewGuid(),
//                    CharacteristicProperties.Read | CharacteristicProperties.Write | CharacteristicProperties.WriteWithoutResponse,
//                    GattPermissions.Read | GattPermissions.Write
//                );
//                var notifyCharacteristic = service.AddCharacteristic
//                (
//                    Guid.NewGuid(),
//                    CharacteristicProperties.Notify,
//                    GattPermissions.Read | GattPermissions.Write
//                );

//                //var descriptor = characteristic.AddDescriptor(Guid.NewGuid(), Encoding.UTF8.GetBytes("Test Descriptor"));

//                notifyCharacteristic.WhenDeviceSubscriptionChanged().Subscribe(e =>
//                {
//                    var @event = e.IsSubscribed ? "Subscribed" : "Unsubcribed";
//                    this.OnEvent($"Device {e.Device.Uuid} {@event}");
//                    this.OnEvent($"Charcteristic Subcribers: {notifyCharacteristic.SubscribedDevices.Count}");

//                    if (this.notifyBroadcast == null)
//                    {
//                        this.OnEvent("Starting Subscriber Thread");
//                        this.notifyBroadcast = Observable
//                            .Interval(TimeSpan.FromSeconds(1))
//                            .Where(x => notifyCharacteristic.SubscribedDevices.Count > 0)
//                            .Subscribe(_ =>
//                            {
//                                try
//                                {
//                                    var dt = DateTime.Now.ToString("g");
//                                    var bytes = Encoding.UTF8.GetBytes(dt);
//                                    notifyCharacteristic
//                                        .BroadcastObserve(bytes)
//                                        .Subscribe(x =>
//                                        {
//                                            var state = x.Success ? "Successfully" : "Failed";
//                                            var data = Encoding.UTF8.GetString(x.Data, 0, x.Data.Length);
//                                            this.OnEvent($"{state} Broadcast {data} to device {x.Device.Uuid} from characteristic {x.Characteristic}");
//                                        });
//                                }
//                                catch (Exception ex)
//                                {
//                                    Debug.WriteLine("Error during broadcast: " + ex);
//                                }
//                            });
//                    }
//                });

//                characteristic.WhenReadReceived().Subscribe(x =>
//                {
//                    var write = this.CharacteristicValue;
//                    if (String.IsNullOrWhiteSpace(write))
//                        write = "(NOTHING)";

//                    x.Value = Encoding.UTF8.GetBytes(write);
//                    this.OnEvent("Characteristic Read Received");
//                });
//                characteristic.WhenWriteReceived().Subscribe(x =>
//                {
//                    var write = Encoding.UTF8.GetString(x.Value, 0, x.Value.Length);
//                    this.OnEvent($"Characteristic Write Received - {write}");
//                });

//                this.server
//                    .WhenRunningChanged()
//                    .Catch<bool, ArgumentException>(ex =>
//                    {
//                        this.dialogs.Alert("Error Starting GATT Server - " + ex);
//                        return Observable.Return(false);
//                    })
//                    .Subscribe(started =>
//                    {
//                        if (!started)
//                        {
//                            this.ServerText = "Start Server";
//                            this.OnEvent("GATT Server Stopped");
//                        }
//                        else
//                        {
//                            this.notifyBroadcast?.Dispose();
//                            this.notifyBroadcast = null;

//                            this.ServerText = "Stop Server";
//                            this.OnEvent("GATT Server Started");
//                            foreach (var s in this.server.Services)
//                            {
//                                this.OnEvent($"Service {s.Uuid} Created");
//                                foreach (var ch in s.Characteristics)
//                                {
//                                    this.OnEvent($"Characteristic {ch.Uuid} Online - Properties {ch.Properties}");
//                                }
//                            }
//                        }
//                    });

//                this.server
//                    .WhenAnyCharacteristicSubscriptionChanged()
//                    .Subscribe(x =>
//                        this.OnEvent($"[WhenAnyCharacteristicSubscriptionChanged] UUID: {x.Characteristic.Uuid} - Device: {x.Device.Uuid} - Subscription: {x.IsSubscribing}")
//                    );

//                //descriptor.WhenReadReceived().Subscribe(x =>
//                //    this.OnEvent("Descriptor Read Received")
//                //);
//                //descriptor.WhenWriteReceived().Subscribe(x =>
//                //{
//                //    var write = Encoding.UTF8.GetString(x.Value, 0, x.Value.Length);
//                //    this.OnEvent($"Descriptor Write Received - {write}");
//                //});
//            }
//            catch (Exception ex)
//            {
//                this.dialogs.Alert("Error building gatt server - " + ex);
//            }
//        }


//        void OnEvent(string msg)
//        {
//            Device.BeginInvokeOnMainThread(() =>
//                this.Output += msg + Environment.NewLine + Environment.NewLine
//            );
//        }


//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }
//}
