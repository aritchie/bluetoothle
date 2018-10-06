using System;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;


namespace Samples.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();
            this.LoadApplication(new App(new PlatformInitializer()));
            new Acr.XamForms.Behaviors.ItemTappedCommandBehavior();
            //UIApplication.SharedApplication.IdleTimerDisabled = false;
            return base.FinishedLaunching(app, options);
        }
    }
}
