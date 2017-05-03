using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Services;
using Xamarin.Forms;


namespace Samples.ViewModels.TestCases
{
    public class Test1ViewModel : AbstractRootViewModel, ITestCaseViewModel
    {
        static readonly Guid ScratchServiceUuid = Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE");
        IDisposable scan;
        IDevice device;


        public Test1ViewModel(ICoreServices services) : base(services)
        {
            this.Run = ReactiveCommand.Create(() =>
            {
                if (this.scan == null)
                {
                    this.scan = this.BleAdapter
                        .ScanWhenAdapterReady()
                        //.Where(x => x.AdvertisementData.ServiceUuids.Any(y => y.Equals(ScratchServiceUuid)))
                        .Where(x => x.Device?.Name.StartsWith("bean", StringComparison.CurrentCultureIgnoreCase) ?? false)
                        .Take(1)
                        .Select(x =>
                        {
                            this.device = x.Device;
                            x.Device.Connect().Subscribe();
                            return x.Device.GetKnownService(ScratchServiceUuid);
                        })
                        .Switch()
                        .Select(x => x.WhenCharacteristicDiscovered())
                        .Switch()
                        .Select(x => x.SubscribeToNotifications())
                        .Switch()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(x =>
                            this.LogItems.Add(new LogItem
                            {
                                Text = x.Characteristic.Uuid.ToString(),
                                Detail = UTF8Encoding.UTF8.GetString(x.Data, 0, x.Data.Length)
                            })
                        );
                }
                else
                {
                    this.scan?.Dispose();
                    this.scan = null;
                    this.device?.CancelConnection();
                }
            });
        }


        public string Name { get; } = "PunchThrough Bean+ - Two Characteristic Subscriptions";
        public ICommand Run { get; }
        public ObservableCollection<LogItem> LogItems { get; } = new ObservableCollection<LogItem>();
    }
}
/*
Service (ad-data) - A495FF20-C5B1-4B44-B512-1370F02D74DE

// start count
Scratch 1 - A495FF21-C5B1-4B44-B512-1370F02D74DE

// temp
Scratch 2 - A495FF22-C5B1-4B44-B512-1370F02D74DE

// accel X
Scratch 3 - A495FF23-C5B1-4B44-B512-1370F02D74DE

// accel Y
Scratch 4 - A495FF24-C5B1-4B44-B512-1370F02D74DE

// accel Z
Scratch 5 - A495FF25-C5B1-4B44-B512-1370F02D74DE
*/
