using System;
using System.Threading.Tasks;
using Plugin.BluetoothLE;
using Prism.Navigation;


namespace Samples
{
    public static class NavExtensions
    {
        public static Task NavToDevice(this INavigationService navigator, IDevice device)
            => navigator.Navigate("DevicePage", new NavigationParameters
            {
                { nameof(device), device }
            });


        public static Task NavToAdapter(this INavigationService navigator, IAdapter adapter)
            => navigator.Navigate("AdapterPage", new NavigationParameters
            {
                { nameof(adapter), adapter }
            });


        public static async Task Navigate(this INavigationService navigator, string page, INavigationParameters parameters)
        {
            var result = await navigator.NavigateAsync(page, parameters);
            if (!result.Success)
                Console.WriteLine(result.Exception);
        }
    }
}
