using System;
using Plugin.BluetoothLE;
using Xamarin.Forms;


namespace Samples.Ble
{
    public class App : Application
    {
        public App()
        {
            this.MainPage = CrossBleAdapter.AdapterScanner.IsSupported
                ? new NavigationPage(new AdapterListPage())
                : new NavigationPage(new AdapterPage());
        }
    }
}