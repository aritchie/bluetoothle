using System;
using System.Reflection;
using Xunit.Runners.UI;


namespace Plugin.BluetoothLE.Uwp.Tests
{
    sealed partial class App : RunnerApplication
    {
        protected override void OnInitializeRunner()
        {
            Acr.Logging.Log.ToDebug();

            this.AddTestAssembly(typeof(Plugin.BluetoothLE.Tests.DeviceTests).GetTypeInfo().Assembly);
            this.AddTestAssembly(this.GetType().GetTypeInfo().Assembly);
        }
    }
}
