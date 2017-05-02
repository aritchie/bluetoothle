using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Services;


namespace Samples.ViewModels.TestCases
{
    public class Test1ViewModel : AbstractRootViewModel, ITestCaseViewModel
    {
        static readonly Guid ScratchServiceUuid = Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE");

        public Test1ViewModel(ICoreServices services) : base(services)
        {
            //this.Run = ReactiveCommand.CreateFromObservable(this.BleAdapter
            //    .ScanWhenAdapterReady()
            //    .Where(x => x.AdvertisementData.ServiceUuids.Any(y => y.Equals(ScratchServiceUuid)))
            //    .Take(1)
            //    .Select(result =>
            //    {
            //        var ob = result.Device
            //            .WhenStatusChanged()
            //            .Where(status => status == ConnectionStatus.Connected)
            //            .Select(_ => result.Device);
            //        result.Device.Connect();
            //        return ob;
            //        //dev.Device.GetKnownService(null);
            //    })
            //    .Switch()
            //    .Select(dev => dev
            //        .GetKnownService(ScratchServiceUuid)
            //        .Select(x => x.WhenCharacteristicDiscovered())
            //    )
            //);
        }


        public string Name { get; } = "PunchThrough Bean+ - Two Characteristic Subscriptions";
        public ICommand Run { get; }
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
