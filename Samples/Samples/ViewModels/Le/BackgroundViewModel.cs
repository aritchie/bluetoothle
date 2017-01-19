using System;
using System.Reactive.Linq;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Services;


namespace Samples.ViewModels.Le
{
    public class BackgroundViewModel : AbstractViewModel
    {
        public BackgroundViewModel(IAppSettings settings, IUserDialogs dialogs)
        {
            this.ServiceUuid = settings.BackgroundScanServiceUuid.ToString();
            this.IsEnabled = settings.EnableBackgroundScan;

            this.WhenAnyValue(x => x.IsEnabled)
                .Skip(1)
                .Subscribe(x =>
                {
                    if (!x)
                    {
                        settings.EnableBackgroundScan = false;
                    }
                    else
                    {
                        var uuid = Guid.Empty;
                        if (!Guid.TryParse(this.ServiceUuid, out uuid))
                        {
                            dialogs.Alert("Invalid UUID");
                        }
                        else
                        {
                            settings.EnableBackgroundScan = true;
                            settings.BackgroundScanServiceUuid = uuid;
                            dialogs.Alert("Background Settings Updated", "Success");
                        }
                    }
                });
        }


        [Reactive] public string ServiceUuid { get; set; }
        [Reactive] public bool IsEnabled { get; set; }
    }
}
