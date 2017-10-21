using System;
using System.Reflection;
using Plugin.BluetoothLE.Tests;
using Xunit.Runners.UI;


namespace Plugin.BluetoothLE.Uwp.Tests
{

    sealed partial class App : RunnerApplication
    {
        protected override void OnInitializeRunner()
        {
            this.AddTestAssembly(typeof(AdapterTests).GetTypeInfo().Assembly);
            this.AddTestAssembly(this.GetType().GetTypeInfo().Assembly);
        }
    }
}
