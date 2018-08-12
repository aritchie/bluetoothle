using System;
using System.Threading.Tasks;
using Plugin.BluetoothLE;
using Prism.Navigation;


namespace Samples
{
    public static class NavExtensions
    {
        public static Task NavToDevice(this INavigationService navigator, IDevice device)
            => navigator.NavigateAsync("DevicePage", new NavigationParameters
            {
                { nameof(device), device }
            });


        public static Task NavToAdapter(this INavigationService navigator, IAdapter adapter)
            => navigator.NavigateAsync("AdapterPage", new NavigationParameters
            {
                { nameof(adapter), adapter }
            });
    }
}
